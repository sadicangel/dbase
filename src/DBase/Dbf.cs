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
    public DateOnly LastUpdate => _header.LastUpdate;

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
            if (_header.RecordCount == value) return;
            _header = _header with
            {
                RecordCount = (uint)value,
                LastUpdate = DateOnly.FromDateTime(DateTime.Now),
            };
            _dirty = true;
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

        var (header, descriptors, backlink) = ReadHeader(dbf);
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
        DbfLanguage language = DbfLanguage.Ansi)
    {
        var dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        var memo = default(FileStream);
        if (!descriptors.GetTableFlags().HasFlag(DbfTableFlags.HasMemoField))
        {
            return Create(dbf, descriptors, memo, version, language);
        }

        var extension = version.IsFoxPro() ? "fpt" : "dbt";
        memo = new FileStream(
            Path.ChangeExtension(
                fileName,
                extension),
            FileMode.CreateNew,
            FileAccess.ReadWrite);

        return Create(dbf, descriptors, memo, version, language);
    }

    internal static Dbf Create(
        Stream dbf,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Stream? memo = null,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.Ansi)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var header = new DbfHeader(descriptors, version, language);

        WriteHeader(dbf, in header, descriptors);

        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Create(memo, version) : null);
    }

    /// <summary>
    /// Saves the current <see cref="Dbf"/> to the specified file, and it's associated <see cref="DBase.Memo"/> if it exists.
    /// </summary>
    /// <remarks>If the current state does not include a memo, only the main file is created. Otherwise, an
    /// additional memo file is created with an extension determined by the version.</remarks>
    /// <param name="fileName">The name of the file to which the current state will be saved.</param>
    public void SaveAs(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        using var dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        if (Memo is null)
        {
            WriteTo(dbf, null);
            return;
        }

        using var memo = new FileStream(Path.ChangeExtension(fileName, Version.IsFoxPro() ? "fpt" : "dbt"), FileMode.CreateNew, FileAccess.ReadWrite);
        WriteTo(dbf, memo);
    }

    /// <summary>
    /// Writes the current <see cref="Dbf"/> to the specified output stream, and optionally writes associated
    /// <see cref="DBase.Memo"/> to a separate stream if provided.
    /// </summary>
    /// <param name="dbf">The output stream to which the main data is written. This parameter cannot be null.</param>
    /// <param name="memo">An optional stream to which memo data is written if both this parameter and the internal memo data are not null.</param>
    public void WriteTo(Stream dbf, Stream? memo)
    {
        ArgumentNullException.ThrowIfNull(dbf);
        Flush();
        _dbf.Position = 0;
        _dbf.CopyTo(dbf);
        if (Memo is not null && memo is not null)
            Memo.WriteTo(memo);
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

    internal static (DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors, DbcBacklink dbcBackLink) ReadHeader(Stream dbf)
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

        var dbcBackLink = DbcBacklink.Empty;
        if (header.Version.IsFoxPro())
        {
            dbf.ReadExactly(dbcBackLink);
        }

        return (header, builder.ToImmutable(), dbcBackLink);

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

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The <see cref="DbfRecord"/> at the specified index.</returns>
    public DbfRecord GetRecord(int index) => ReadRecord<DbfRecord>(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns></returns>
    public T GetRecord<T>(int index) => ReadRecord<T>(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Enumerates all records in the database.
    /// </summary>
    /// <returns>The sequence of records in the database.</returns>
    public IEnumerable<DbfRecord> EnumerateRecords() => EnumerateRecords<DbfRecord>();

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

    /// <summary>
    /// Adds a new record to the database.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void Add(DbfRecord record) => WriteRecord(RecordCount, record);

    /// <summary>
    /// Adds a new record to the database.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="record">The record to add.</param>
    public void Add<T>(T record) => WriteRecord(RecordCount, record);

    internal long SetStreamPositionForRecord(int recordIndex) =>
        _dbf.Position = _header.HeaderLength + recordIndex * _header.RecordLength;

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

        record = Descriptors.GetSerializer<T>().Deserialize(buffer.Span, new DbfSerializationContext(Encoding, Memo, DecimalSeparator));

        return true;
    }

    internal void WriteRecord<T>(int index, T record)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, RecordCount);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        Descriptors.GetSerializer<T>().Serialize(buffer.Span, record, new DbfSerializationContext(Encoding, Memo, DecimalSeparator));
        _dbf.Write(buffer.Span);

        RecordCount = Math.Max(RecordCount, index + 1);
    }
}
