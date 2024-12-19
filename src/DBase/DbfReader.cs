using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace DBase;

public sealed class DbfReader : IDisposable
{
    private readonly Stream _stream;
    private readonly DbfHeader _header;

    public ref readonly DbfHeader Header => ref _header;
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }
    public Encoding Encoding { get; }
    public char DecimalSeparator { get; }

    public DbfReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;

        if (_stream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _header, 1))) != DbfHeader.Size)
        {
            throw new InvalidDataException("Invalid DBF header");
        }

        var descriptors = new DbfFieldDescriptor[(_header.HeaderLength - DbfHeader.Size) / DbfFieldDescriptor.Size];
        if (_stream.Read(MemoryMarshal.AsBytes(descriptors.AsSpan())) != descriptors.Length * DbfFieldDescriptor.Size)
        {
            throw new InvalidDataException("Invalid DBF field descriptors");
        }
        Descriptors = ImmutableCollectionsMarshal.AsImmutableArray(descriptors);

        if (_stream.ReadByte() != 0x0D)
        {
            throw new InvalidDataException("Invalid DBF header terminator");
        }

        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();
    }

    public void Dispose() => _stream.Dispose();

    public bool Read([MaybeNullWhen(false)] out DbfRecord record)
    {
        byte[]? pooledArray = null;
        try
        {
            var recordLength = _header.RecordLength;
            Span<byte> buffer = recordLength < 256
                ? stackalloc byte[recordLength]
                : (pooledArray = ArrayPool<byte>.Shared.Rent(recordLength)).AsSpan(0, recordLength);

            if (_stream.Read(buffer) == recordLength)
            {
                record = ReadRecord(buffer, Descriptors, Encoding, DecimalSeparator);
                return true;
            }
            else
            {
                record = default;
                return false;
            }
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
                ReadOnlySpan<byte> trims = [(byte)'\0', (byte)' '];
                var @string = source.Trim(trims);
                return @string.Length > 0 ? encoding.GetString(@string) : string.Empty;

            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                Span<char> integer = source.Length <= 40 ? stackalloc char[source.Length] : new char[source.Length];
                encoding.GetChars(source, integer);
                return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                Span<char> @double = source.Length <= 40 ? stackalloc char[source.Length] : new char[source.Length];
                encoding.GetChars(source, @double);
                if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx && idx >= 0)
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
                    '?' or ' ' => null,
                    'T' or 'Y' or '1' => true,
                    'F' or 'N' or '0' => false,
                    _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
                };

            case DbfFieldType.Ole:
            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
                return encoding.GetString(source);

            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }
    }
}
