using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DBase.Internal;
using DotNext.Buffers;

namespace DBase;

public sealed class Dbf : IDisposable, IReadOnlyList<DbfRecord>
{
    private const int StackallocThreshold = 256;

    private readonly Stream _dbf;
    private DbfHeader _header;
    private bool _dirty;

    public ref readonly DbfHeader Header => ref _header;
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }
    public Encoding Encoding { get; }
    public char DecimalSeparator { get; }

    public Memo? Memo { get; }

    public int Count
    {
        get => (int)_header.RecordCount;
        private set
        {
            if (_header.RecordCount != value)
            {
                _header = _header with
                {
                    RecordCount = (uint)value,
                    LastUpdate = DateOnly.FromDateTime(DateTime.Now)
                };
                _dirty = true;
            }
        }
    }

    public DbfRecord this[int index] { get => ReadRecord(index); }

    private Dbf(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors, Memo? memo)
    {
        _dbf = dbf;
        _header = header;
        Descriptors = descriptors;
        descriptors.EnsureFieldOffsets();

        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();

        Memo = memo;
    }

    public static Dbf Open(string fileName)
    {
        var dbf = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
        var dbtName = Path.ChangeExtension(fileName, "dbt");
        var memo = File.Exists(dbtName) ? new FileStream(dbtName, FileMode.Open, FileAccess.ReadWrite) : null;
        if (memo is null)
        {
            var fptName = Path.ChangeExtension(fileName, "fpt");
            memo = File.Exists(fptName) ? new FileStream(fptName, FileMode.Open, FileAccess.ReadWrite) : null;
        }
        return Open(dbf, memo);
    }

    public static Dbf Open(Stream dbf, Stream? memo = null)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var (header, descriptors) = ReadHeader(dbf);
        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Open(memo, header.Version) : null);
    }

    public static Dbf Create(
        string fileName,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.ANSI)
    {
        var dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        var memo = default(FileStream);
        if (descriptors.GetTableFlags().HasFlag(DbfTableFlags.HasMemoField))
        {
            if (version.IsFoxPro())
            {
                memo = new FileStream(Path.ChangeExtension(fileName, "fpt"), FileMode.CreateNew, FileAccess.ReadWrite);
            }
            else
            {
                memo = new FileStream(Path.ChangeExtension(fileName, "dbt"), FileMode.CreateNew, FileAccess.ReadWrite);
            }
        }
        return Create(dbf, descriptors, memo, version, language);
    }

    public static Dbf Create(
        Stream dbf,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Stream? memo = null,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.ANSI)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var header = new DbfHeader
        {
            HeaderLength = (ushort)(DbfHeader.Size + descriptors.Length * DbfFieldDescriptor.Size + 1),
            Language = language,
            LastUpdate = DateOnly.FromDateTime(DateTime.Now),
            RecordCount = 0,
            RecordLength = (ushort)descriptors.Sum(static d => d.Length),
            TableFlags = descriptors.GetTableFlags(),
            Version = version,
        };

        WriteHeader(dbf, in header, descriptors);

        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Create(memo, version) : null);
    }

    public void Dispose()
    {
        Flush();
        Memo?.Dispose();
        _dbf.Dispose();
    }

    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeader(_dbf, in _header, Descriptors);
        }
        Memo?.Flush();
        _dbf.Flush();
    }

    internal static (DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors) ReadHeader(Stream dbf)
    {
        dbf.Position = 0;
        var version = (DbfVersion)dbf.ReadByte();
        dbf.Position = 0;

        if (version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'");
        }

        Unsafe.SkipInit(out DbfHeader header);
        if (version is DbfVersion.DBase02)
        {
            Unsafe.SkipInit(out DbfHeader02 header02);
            dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header02, 1)));
            header = header02;
        }
        else
        {
            dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
        }

        if (header.Version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        var builder = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(initialCapacity: 8);

        if (header.Version is DbfVersion.DBase02)
        {
            dbf.Position = DbfHeader02.Size;
            while (TryReadDescriptor(dbf, out DbfFieldDescriptor02 descriptor))
                builder.Add(descriptor);
            dbf.Position = DbfHeader02.Size + builder.Count * DbfFieldDescriptor02.Size;
        }
        else
        {
            dbf.Position = DbfHeader.Size;
            while (TryReadDescriptor(dbf, out DbfFieldDescriptor descriptor))
                builder.Add(descriptor);
            dbf.Position = DbfHeader.Size + builder.Count * DbfFieldDescriptor.Size;
        }

        if (dbf.ReadByte() is not 0x0D)
            throw new InvalidDataException("Invalid DBF header terminator");

        return (header, builder.ToImmutable());

        static bool TryReadDescriptor<T>(Stream dbf, out T descriptor) where T : unmanaged
        {
            Unsafe.SkipInit(out descriptor);
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref descriptor, 1));
            var bytesRead = dbf.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return bytesRead == buffer.Length && buffer[0] is not 0x0D;
        }
    }

    internal static void WriteHeader(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        if (header.Version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        dbf.Position = 0;

        if (header.Version is DbfVersion.DBase02)
        {
            DbfHeader02 header02 = header;
            dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header02, 1)));
            foreach (var descriptor in descriptors)
            {
                DbfFieldDescriptor02 descriptor02 = descriptor;
                dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref descriptor02, 1)));
            }
            dbf.WriteByte(0x0D);
            if (dbf.Position is not DbfHeader02.HeaderLengthInDisk)
            {
                dbf.Position = DbfHeader02.HeaderLengthInDisk - 1;
                dbf.WriteByte(0x00);
            }
        }

        else
        {
            dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in header, 1)));
            dbf.Write(MemoryMarshal.AsBytes(descriptors.AsSpan()));
            dbf.WriteByte(0x0D);
        }
    }

    internal long SetStreamPositionForRecord(int recordIndex) =>
        _dbf.Position = _header.HeaderLength + recordIndex * _header.RecordLength;

    internal long SetStreamPositionForField(int recordIndex, int fieldIndex) =>
        _dbf.Position = _header.HeaderLength + recordIndex * _header.RecordLength + Descriptors[fieldIndex].Offset;

    internal DbfRecord ReadRecord(int recordIndex) =>
        ReadRecord(recordIndex, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(recordIndex));

    internal bool ReadRecord(int recordIndex, out DbfRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);

        record = default;

        if (recordIndex >= Count)
        {
            return false;
        }

        SetStreamPositionForRecord(recordIndex);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        var bytesRead = _dbf.ReadAtLeast(buffer.Span, _header.RecordLength, throwOnEndOfStream: false);
        if (bytesRead != _header.RecordLength)
        {
            return false;
        }

        record = ReadRecord(buffer.Span);

        return true;
    }

    private DbfRecord ReadRecord(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(source.Length, _header.RecordLength);

        var status = (DbfRecordStatus)source[0];
        var fields = ImmutableArray.CreateBuilder<DbfField>(Descriptors.Length);
        foreach (var descriptor in Descriptors)
        {
            var field = ReadField(source.Slice(descriptor.Offset, descriptor.Length), in descriptor);
            fields.Add(field);
        }

        return new DbfRecord(status, fields.MoveToImmutable());
    }

    internal DbfField ReadField(int recordIndex, int fieldIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(recordIndex, Count);

        SetStreamPositionForField(recordIndex, fieldIndex);

        var descriptor = Descriptors[fieldIndex];
        Span<byte> buffer = stackalloc byte[descriptor.Length];
        _dbf.ReadExactly(buffer);
        return ReadField(buffer, in descriptor);
    }

    private DbfField ReadField(ReadOnlySpan<byte> source, in DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => MemoryMarshal.Read<long>(source),
            DbfFieldType.Binary when descriptor.Length == 8 => MemoryMarshal.Read<double>(source),
            DbfFieldType.Binary => ReadMemo(source, MemoRecordType.Object, Encoding, Memo),
            DbfFieldType.Blob => ReadMemo(source, MemoRecordType.Object, Encoding, Memo),
            DbfFieldType.Character => Encoding.GetString(source.Trim([(byte)'\0', (byte)' '])),
            DbfFieldType.Currency => decimal.FromOACurrency(MemoryMarshal.Read<long>(source)),
            DbfFieldType.Date => ReadDate(source, Encoding),
            DbfFieldType.DateTime => ReadDateTime(source),
            DbfFieldType.Double => MemoryMarshal.Read<double>(source),
            DbfFieldType.Float => ReadNumericF64(source, Encoding, DecimalSeparator),
            DbfFieldType.Int32 => MemoryMarshal.Read<int>(source),
            DbfFieldType.Logical => ReadLogical(source, Encoding),
            DbfFieldType.Memo => ReadMemo(source, MemoRecordType.Memo, Encoding, Memo),
            DbfFieldType.NullFlags => Convert.ToHexString(source),
            DbfFieldType.Numeric when descriptor.Decimal == 0 => ReadNumericI64(source, Encoding),
            DbfFieldType.Numeric => ReadNumericF64(source, Encoding, DecimalSeparator),
            DbfFieldType.Ole => ReadMemo(source, MemoRecordType.Object, Encoding, Memo),
            DbfFieldType.Picture => ReadMemo(source, MemoRecordType.Picture, Encoding, Memo),
            DbfFieldType.Timestamp => ReadDateTime(source),
            DbfFieldType.Variant => Encoding.GetString(source[..source[^1]]),
            _ => throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType)),
        };

        static DateTime? ReadDate(ReadOnlySpan<byte> source, Encoding encoding)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.Length != 8) return default;
            Span<char> date = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source[..8], date);
            return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        static DateTime? ReadDateTime(ReadOnlySpan<byte> source)
        {
            var julian = MemoryMarshal.Read<int>(source);
            if (julian is 0) return default;
            var milliseconds = MemoryMarshal.Read<int>(source.Slice(4, 4));
            return DateTime.FromOADate(julian - 2415018.5).AddMilliseconds(milliseconds);
        }

        static bool? ReadLogical(ReadOnlySpan<byte> source, Encoding encoding)
        {
            if (encoding.GetCharCount(source) is not 1) return default;
            var v = '\0';
            encoding.GetChars(source, MemoryMarshal.CreateSpan(ref v, 1));
            return char.ToUpperInvariant(v) switch
            {
                '?' or ' ' => null,
                'T' or 'Y' or '1' => true,
                'F' or 'N' or '0' => false,
                // TODO: Maybe we should just return null?
                _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
            };
        }

        static string ReadMemo(ReadOnlySpan<byte> source, MemoRecordType type, Encoding encoding, Memo? memo)
        {
            if (memo is null || source is [])
            {
                return string.Empty;
            }

            var index = 0;
            if (source.Length != 4)
            {
                Span<char> chars = stackalloc char[encoding.GetCharCount(source)];
                encoding.GetChars(source, chars);
                chars = chars.Trim();
                if (chars is [])
                {
                    return string.Empty;
                }
                index = int.Parse(chars);
            }
            else
            {
                index = MemoryMarshal.Read<int>(source);
            }

            if (index == 0)
            {
                return string.Empty;
            }

            return type is MemoRecordType.Memo
                ? encoding.GetString(memo[index].Span)
                : Convert.ToBase64String(memo[index].Span);
        }

        static long? ReadNumericI64(ReadOnlySpan<byte> source, Encoding encoding)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.IsEmpty) return default;
            Span<char> integer = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, integer);
            return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        static double? ReadNumericF64(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.IsEmpty || (source.Length == 1 && !char.IsAsciiDigit((char)source[0])))
                return default;
            Span<char> @double = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, @double);
            if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
                @double[idx] = '.';
            return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);
        }
    }

    public IEnumerator<DbfRecord> GetEnumerator()
    {
        var index = 0;
        while (ReadRecord(index++, out var record))
        {
            yield return record;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void WriteRecords(int index, params ReadOnlySpan<DbfRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        foreach (var record in records)
        {
            WriteRecord(buffer.Span, record.Status, record.Fields.AsSpan());
            _dbf.Write(buffer.Span);
        }

        Count = index + records.Length;
    }

    internal void WriteRecord(int index, DbfRecordStatus status, params ReadOnlySpan<DbfField> fields)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        ArgumentOutOfRangeException.ThrowIfNotEqual(fields.Length, Descriptors.Length);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        WriteRecord(buffer.Span, status, fields);
        _dbf.Write(buffer.Span);

        Count = Math.Max(Count, index + 1);
    }

    private void WriteRecord(Span<byte> target, DbfRecordStatus status, params ReadOnlySpan<DbfField> fields)
    {
        target[0] = (byte)status;
        for (var i = 0; i < fields.Length; ++i)
        {
            var descriptor = Descriptors[i];
            WriteField(target.Slice(descriptor.Offset, descriptor.Length), in descriptor, fields[i]);
        }
    }

    private void WriteField(Span<byte> target, in DbfFieldDescriptor descriptor, DbfField field)
    {
        target.Fill((byte)' ');
        if (field.IsNull)
        {
            if (descriptor.Type is DbfFieldType.Logical)
                target[0] = (byte)'?';

            return;
        }

        switch (descriptor.Type)
        {
            case DbfFieldType.AutoIncrement:
                MemoryMarshal.Write(target, field.GetValue<long>());
                break;

            case DbfFieldType.Binary when descriptor.Length == 8:
                MemoryMarshal.Write(target, field.GetValue<double>());
                break;

            case DbfFieldType.Binary:
                WriteMemo(target, MemoRecordType.Object, field.GetValue<string>(), Encoding, Memo);
                break;

            case DbfFieldType.Blob:
                _ = Encoding.TryGetBytes(field.GetValue<string>(), target, out _);
                break;

            case DbfFieldType.Character:
                _ = Encoding.TryGetBytes(field.GetValue<string>(), target, out _);
                break;

            case DbfFieldType.Currency:
                MemoryMarshal.Write(target, decimal.ToOACurrency(field.GetValue<decimal>()));
                break;

            case DbfFieldType.Date:
                WriteDate(target, field.GetValue<DateTime>(), Encoding);
                break;

            case DbfFieldType.DateTime:
                WriteDateTime(target, field.GetValue<DateTime>());
                break;

            case DbfFieldType.Double:
                MemoryMarshal.Write(target, field.GetValue<double>());
                break;

            case DbfFieldType.Float:
                WriteNumericF64(target, field.GetValue<double>(), in descriptor, Encoding, DecimalSeparator);
                break;

            case DbfFieldType.Int32:
                MemoryMarshal.Write(target, field.GetValue<int>());
                break;

            case DbfFieldType.Logical:
                target[0] = field.GetValue<bool>() ? (byte)'T' : (byte)'F';
                break;

            case DbfFieldType.Memo:
                WriteMemo(target, MemoRecordType.Memo, field.GetValue<string>(), Encoding, Memo);
                break;

            case DbfFieldType.NullFlags:
                Convert.FromHexString(field.GetValue<string>(), target, out _, out _);
                break;

            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                WriteNumericI64(target, field.GetValue<long>(), Encoding);
                break;

            case DbfFieldType.Numeric:
                WriteNumericF64(target, field.GetValue<double>(), in descriptor, Encoding, DecimalSeparator);
                break;

            case DbfFieldType.Ole:
                WriteMemo(target, MemoRecordType.Object, field.GetValue<string>(), Encoding, Memo);
                break;

            case DbfFieldType.Picture:
                WriteMemo(target, MemoRecordType.Picture, field.GetValue<string>(), Encoding, Memo);
                break;

            case DbfFieldType.Timestamp:
                WriteDateTime(target, field.GetValue<DateTime>());
                break;

            case DbfFieldType.Variant:
                WriteVariant(target, field.GetValue<string>(), Encoding);
                break;

            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }

        static void WriteDate(Span<byte> target, DateTime dateTime, Encoding encoding)
        {
            Span<char> chars = stackalloc char[8];
            _ = dateTime.TryFormat(chars, out _, "yyyyMMdd", CultureInfo.InvariantCulture);
            _ = encoding.TryGetBytes(chars, target, out _);
        }

        static void WriteDateTime(Span<byte> source, DateTime dateTime)
        {
            var julian = (int)(dateTime.Date.ToOADate() + 2415018.5);
            MemoryMarshal.Write(source, julian);
            var milliseconds = (int)dateTime.TimeOfDay.TotalMilliseconds;
            MemoryMarshal.Write(source.Slice(4, 4), milliseconds);
        }

        static void WriteMemo(Span<byte> target, MemoRecordType type, string field, Encoding encoding, Memo? memo)
        {
            if (memo is null)
            {
                return;
            }

            var index = memo.NextIndex;
            var data = type is MemoRecordType.Memo ? encoding.GetBytes(field) : Convert.FromBase64String(field);
            memo.Add(new MemoRecord(type, data));

            if (target.Length != 4)
            {
                Span<char> chars = stackalloc char[10];
                index.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
                var bytesRequired = encoding.GetByteCount(chars[..charsWritten]);
                encoding.TryGetBytes(chars[..charsWritten], target[Math.Max(0, 10 - bytesRequired)..], out _);
            }
            else
            {
                MemoryMarshal.Write(target, index);
            }
        }

        static void WriteNumericI64(Span<byte> target, long i64, Encoding encoding)
        {
            Span<char> @long = stackalloc char[20];
            if (!i64.TryFormat(@long, out var charsWritten, "D", CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Failed to format value '{i64}' as '{DbfFieldType.Numeric}'");
            _ = encoding.TryGetBytes(@long, target, out _);
        }

        static void WriteNumericF64(Span<byte> target, double f64, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator)
        {
            Span<char> format = ['F', '\0', '\0'];
            if (!descriptor.Decimal.TryFormat(format[1..], out var charsWritten))
                throw new InvalidOperationException($"Failed to create decimal format");
            format = format[..(1 + charsWritten)];

            Span<char> chars = stackalloc char[20];
            if (!f64.TryFormat(chars, out charsWritten, format, CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Failed to format value '{f64}' for field type '{descriptor.Type}'");

            if (decimalSeparator is not '.' && chars.IndexOf(decimalSeparator) is var idx and >= 0)
                chars[idx] = decimalSeparator;

            _ = encoding.TryGetBytes(chars[..charsWritten], target, out _);
        }

        static void WriteVariant(Span<byte> target, string field, Encoding encoding)
        {
            _ = encoding.TryGetBytes(field, target[..^1], out var bytesWritten);
            target[^1] = (byte)bytesWritten;
        }
    }
}
