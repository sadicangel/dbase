using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using DBase.Interop;
using DBase.Serialization;
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
            RecordLength = (ushort)(1 + descriptors.Sum(static d => d.Length)),
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

        if (!Enum.IsDefined(version))
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'");
        }

        var header = version is DbfVersion.DBase02
            ? (DbfHeader)dbf.Read<DbfHeader02>()
            : dbf.Read<DbfHeader>();

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

        static bool TryReadDescriptor<T>(Stream dbf, out T descriptor) where T : unmanaged =>
            dbf.TryRead(out descriptor) && Unsafe.As<T, byte>(ref descriptor) is not 0x0D;
    }

    internal static void WriteHeader(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        if (!Enum.IsDefined(header.Version))
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        dbf.Position = 0;

        if (header.Version is DbfVersion.DBase02)
        {
            dbf.Write((DbfHeader02)header);
            foreach (var descriptor in descriptors)
            {
                dbf.Write((DbfFieldDescriptor02)descriptor);
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
            dbf.Write(header);
            foreach (var descriptor in descriptors)
            {
                dbf.Write(descriptor);
            }
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

        record = DbfMarshal.ReadRecord(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo);

        return true;
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
            DbfMarshal.WriteRecord(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo, record);
            _dbf.Write(buffer.Span);
        }

        Count = index + records.Length;
    }

    internal void WriteRecord(int index, DbfRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Count, Descriptors.Length);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        DbfMarshal.WriteRecord(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo, record);
        _dbf.Write(buffer.Span);

        Count = Math.Max(Count, index + 1);
    }

    internal void WriteRecord<T>(int index, T record, DbfRecordStatus status)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        SetStreamPositionForRecord(index);

        var serializer = DbfRecordSerializer.GetSerializer<T>(Descriptors.AsSpan());

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        serializer(buffer.Span, record, status, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo);
        _dbf.Write(buffer.Span);

        Count = Math.Max(Count, index + 1);
    }

    public void Add(DbfRecord record) =>
        WriteRecord(Count, record);

    public void Add<T>(T record, DbfRecordStatus status = DbfRecordStatus.Valid) =>
        WriteRecord(Count, record, status);
}
