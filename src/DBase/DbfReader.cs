using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DBase.Internal;

namespace DBase;

public sealed class DbfReader : IDisposable
{
    private readonly Stream _stream;
    private readonly DbfHeader _header;
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly long _eofPosition;

    public ref readonly DbfHeader Header => ref _header;
    public ImmutableArray<DbfFieldDescriptor> Descriptors => _descriptors;
    public Encoding Encoding { get; }
    public char DecimalSeparator { get; }

    public DbfReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;

        _stream.Position = 0;
        var version = (DbfVersion)_stream.ReadByte();
        _stream.Position = 0;

        (_header, _descriptors) = version switch
        {
            DbfVersion.DBase02 =>
                ReadHeaderDBase02(stream),
            DbfVersion.DBase03 or DbfVersion.DBase04 or DbfVersion.DBase05 or DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.DBase43 or DbfVersion.DBase63 or DbfVersion.DBase83 or DbfVersion.DBase8B or DbfVersion.DBaseCB or DbfVersion.FoxPro2WithMemo or DbfVersion.FoxBASE =>
                ReadHeader(stream),
            _ =>
                throw new InvalidDataException($"Invalid DBF version '0x{(byte)version:X2}'"),
        };

        _eofPosition = _header.HeaderLength + _header.RecordCount * _header.RecordLength;
        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();
    }

    public void Dispose() => _stream.Dispose();

    private static (DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors) ReadHeader(Stream stream)
    {
        Unsafe.SkipInit(out DbfHeader header);
        stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
        var descriptors03 = new DbfFieldDescriptor[(header.HeaderLength - DbfHeader.Size) / DbfFieldDescriptor.Size];
        stream.ReadExactly(MemoryMarshal.AsBytes(descriptors03.AsSpan()));

        if (stream.ReadByte() != 0x0D)
        {
            throw new InvalidDataException("Invalid DBF header terminator");
        }

        var descriptors = ImmutableCollectionsMarshal.AsImmutableArray(descriptors03);

        return (header, descriptors);
    }

    private static (DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors) ReadHeaderDBase02(Stream stream)
    {
        Unsafe.SkipInit(out DbfHeader02 header02);
        stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header02, 1)));
        var descriptors02 = ArrayPool<DbfFieldDescriptor02>.Shared.Rent(32);
        stream.ReadExactly(MemoryMarshal.AsBytes(descriptors02.AsSpan(0, 32)));

        var count = 32;
        if (stream.ReadByte() is not 0x0D)
        {
            count = Array.FindIndex(descriptors02, static descriptor => descriptor.Name[0] is 0x0D);
            if (count < 0) count = 32;
        }

        var descriptors = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(count);
        for (var i = 0; i < count; ++i)
            descriptors.Add(descriptors02[i]);

        ArrayPool<DbfFieldDescriptor02>.Shared.Return(descriptors02);

        return (header02, descriptors.MoveToImmutable());
    }

    public bool Read([MaybeNullWhen(false)] out DbfRecord record)
    {
        record = null;

        if (_stream.Position >= _eofPosition)
        {
            return false;
        }

        byte[]? pooledArray = null;
        try
        {
            var recordLength = _header.RecordLength;
            Span<byte> buffer = recordLength < 256
                ? stackalloc byte[recordLength]
                : (pooledArray = ArrayPool<byte>.Shared.Rent(recordLength)).AsSpan(0, recordLength);

            var bytesRead = _stream.ReadAtLeast(buffer, recordLength, throwOnEndOfStream: false);
            if (bytesRead != recordLength)
            {
                return false;
            }

            record = ReadRecord(buffer, Descriptors, Encoding, DecimalSeparator);
            return true;
        }
        finally
        {
            if (pooledArray is not null)
                ArrayPool<byte>.Shared.Return(pooledArray);
        }
    }

    public IEnumerable<DbfRecord> Read()
    {
        while (Read(out var record))
        {
            yield return record;
        }
    }

    internal static DbfRecord ReadRecord(ReadOnlySpan<byte> source, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator)
    {
        var fields = new DbfField[descriptors.Length];
        var status = (DbfRecordStatus)source[0];
        var offset = 1;
        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = descriptors[i];
            var field = ReadField(source.Slice(offset, descriptor.Length), in descriptor, encoding, decimalSeparator);
            fields[i] = field;
            offset += descriptor.Length;
        }
        return new DbfRecord(status, ImmutableCollectionsMarshal.AsImmutableArray(fields));
    }

    internal static DbfField ReadField(ReadOnlySpan<byte> source, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator)
    {
        switch (descriptor.Type)
        {
            case DbfFieldType.Character:
                source = source.Trim([(byte)'\0', (byte)' ']);
                return source.Length > 0 ? encoding.GetString(source) : string.Empty;

            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                source = source.Trim([(byte)'\0', (byte)' ']);
                if (source is [])
                    return default(long?);
                Span<char> integer = stackalloc char[source.Length];
                encoding.GetChars(source, integer);
                return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                source = source.Trim([(byte)'\0', (byte)' ']);
                if (source is [] || source is [var b] && !char.IsAsciiDigit((char)b))
                    return default(double?);
                Span<char> @double = stackalloc char[source.Length];
                encoding.GetChars(source, @double);
                if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
                    @double[idx] = '.';
                return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);

            case DbfFieldType.Int32:
            case DbfFieldType.AutoIncrement:
                return MemoryMarshal.Read<int>(source);

            case DbfFieldType.Double:
                return MemoryMarshal.Read<double>(source);

            case DbfFieldType.Date:
                Span<char> date = stackalloc char[8];
                encoding.GetChars(source[..8], date);
                return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);

            case DbfFieldType.Timestamp:
                return DateTime.FromOADate(MemoryMarshal.Read<int>(source[..4]) - 2415018.5) + TimeSpan.FromMilliseconds(MemoryMarshal.Read<int>(source.Slice(4, 4)));

            case DbfFieldType.Logical:
                var l = '\0';
                encoding.GetChars(source[..1], MemoryMarshal.CreateSpan(ref l, 1));
                return char.ToUpperInvariant(l) switch
                {
                    '?' or ' ' => default(bool?),
                    'T' or 'Y' or '1' => true,
                    'F' or 'N' or '0' => false,
                    _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
                };

            case DbfFieldType.Ole:
            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
                // TODO: Actually read data from the memo file using this value as the memo index.
                return encoding.GetString(source);

            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }
    }
}
