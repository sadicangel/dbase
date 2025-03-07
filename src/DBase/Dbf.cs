using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using DBase.Interop;
using DBase.Serialization;
using DotNext.Buffers;

namespace DBase;

/// <summary>
/// Represents a dBASE database file.
/// </summary>
/// <remarks>This class cannot be inherited.</remarks>
public sealed class Dbf : IDisposable
{
    private const int StackallocThreshold = 256;

    private readonly Stream _dbf;
    private DbfHeader _header;
    private bool _dirty;

    /// <summary>
    /// Gets the version of the dBASE database file.
    /// </summary>
    public DbfVersion Version => _header.Version;

    /// <summary>
    /// Gets the language of the dBASE database file.
    /// </summary>
    public DbfLanguage Language => _header.Language;

    /// <summary>
    /// Gets the encoding used to read and write text.
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the decimal separator used to read and write numeric values.
    /// </summary>
    public char DecimalSeparator { get; }

    /// <summary>
    /// Gets or sets the date of the last update to the database.
    /// </summary>
    public DateOnly LastUpdate
    {
        get
        {
            var year = 1900 + _header.LastUpdateYear;
            var month = int.Clamp(_header.LastUpdateMonth, 1, 12);
            var day = int.Clamp(_header.LastUpdateDay, 1, DateTime.DaysInMonth(year, month));
            return new(year, month, day);
        }
    }

    /// <summary>
    /// Gets the length of the header in bytes.
    /// </summary>
    public int HeaderLength => _header.HeaderLength;

    /// <summary>
    /// Gets the length of each record in bytes.
    /// </summary>
    public int RecordLength => _header.RecordLength;

    /// <summary>
    /// Gets the number of records in the database.
    /// </summary>
    public int RecordCount
    {
        get => (int)_header.RecordCount;
        private set
        {
            if (_header.RecordCount != value)
            {
                var now = DateTime.Now;
                _header = _header with
                {
                    RecordCount = (uint)value,
                    LastUpdateYear = (byte)(now.Year - 1900),
                    LastUpdateMonth = (byte)now.Month,
                    LastUpdateDay = (byte)now.Day,
                };
                _dirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the field descriptors that define the record structure.
    /// </summary>
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets the memo file associated with this database file.
    /// </summary>
    public Memo? Memo { get; }

    /// <summary>
    /// Gets or sets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index"></param>
    /// <returns>
    /// The <see cref="DbfRecord"/> at the specified index.
    /// </returns>
    public DbfRecord this[int index] => GetRecord(index);

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

    /// <summary>
    /// Opens an existing dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to open.</param>
    /// <returns></returns>
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

    internal static Dbf Open(Stream dbf, Stream? memo = null)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var (header, descriptors) = ReadHeader(dbf);
        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Open(memo, header.Version) : null);
    }

    /// <summary>
    /// Creates a new dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="descriptors">The field descriptors that define the record structure.</param>
    /// <param name="version">The version of the dBASE database file.</param>
    /// <param name="language">The language of the dBASE database file.</param>
    /// <returns></returns>
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

    internal static Dbf Create(
        Stream dbf,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Stream? memo = null,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.ANSI)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var now = DateTime.Now;

        var header = new DbfHeader
        {
            HeaderLength = (ushort)(DbfHeader.Size + descriptors.Length * DbfFieldDescriptor.Size + 1),
            Language = language,
            LastUpdateYear = (byte)(now.Year - 1900),
            LastUpdateMonth = (byte)now.Month,
            LastUpdateDay = (byte)now.Day,
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

    /// <summary>
    /// Closes the database file and releases any resources associated with it.
    /// </summary>
    public void Dispose()
    {
        Flush();
        Memo?.Dispose();
        _dbf.Dispose();
    }

    /// <summary>
    /// Flushes the database file to disk.
    /// </summary>
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

    internal bool ReadRecord(int recordIndex, out DbfRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);

        record = default;

        if (recordIndex >= RecordCount)
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

    internal bool ReadRecord<T>(int recordIndex, [MaybeNullWhen(false)] out T record)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);

        record = default;

        if (recordIndex >= RecordCount)
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

        var deserializer = DbfRecordSerializer.GetDeserializer<T>(Descriptors.AsSpan());

        record = deserializer(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo);

        return true;
    }

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The <see cref="DbfRecord"/> at the specified index.</returns>
    public DbfRecord GetRecord(int index) =>
        ReadRecord(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns></returns>
    public T GetRecord<T>(int index) =>
        ReadRecord<T>(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Enumerates all records in the database.
    /// </summary>
    /// <returns>The sequence of records in the database.</returns>
    public IEnumerable<DbfRecord> EnumerateRecords()
    {
        var index = 0;
        while (ReadRecord(index++, out var record))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Enumerates all records in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <returns>The sequence of records in the database.</returns>
    public IEnumerable<T> EnumerateRecords<T>()
    {
        var index = 0;
        while (ReadRecord<T>(index++, out var record))
        {
            yield return record;
        }
    }

    internal void WriteRecords(int index, params ReadOnlySpan<DbfRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, RecordCount);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        foreach (var record in records)
        {
            DbfMarshal.WriteRecord(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo, record);
            _dbf.Write(buffer.Span);
        }

        RecordCount = index + records.Length;
    }

    internal void WriteRecord(int index, DbfRecord record)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, RecordCount);
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Count, Descriptors.Length);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        DbfMarshal.WriteRecord(buffer.Span, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo, record);
        _dbf.Write(buffer.Span);

        RecordCount = Math.Max(RecordCount, index + 1);
    }

    internal void WriteRecord<T>(int index, T record, DbfRecordStatus status)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, RecordCount);

        SetStreamPositionForRecord(index);

        var serializer = DbfRecordSerializer.GetSerializer<T>(Descriptors.AsSpan());

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        serializer(buffer.Span, record, status, Descriptors.AsSpan(), Encoding, DecimalSeparator, Memo);
        _dbf.Write(buffer.Span);

        RecordCount = Math.Max(RecordCount, index + 1);
    }

    /// <summary>
    /// Adds a new record to the database.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void Add(DbfRecord record) =>
        WriteRecord(RecordCount, record);

    /// <summary>
    /// Adds a new record to the database.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="record">The record to add.</param>
    /// <param name="status">The status of the record.</param>
    public void Add<T>(T record, DbfRecordStatus status = DbfRecordStatus.Valid) =>
        WriteRecord(RecordCount, record, status);
}
