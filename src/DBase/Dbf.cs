using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DBase.Interop;
using DBase.Serialization;
using DotNext.Buffers;

namespace DBase;

/// <summary>
/// Represents an open DBF table and optional memo file.
/// </summary>
/// <remarks>
/// This type provides record-level read and write operations for DBF data and keeps the file header
/// synchronized when record count changes.
/// </remarks>
public sealed class Dbf : IDisposable
{
    private const int StackallocThreshold = 256;

    private readonly Stream _dbf;
    private DbfHeader _header;
    private bool _dirty;

    /// <summary>
    /// Gets the DBF format version stored in the file header.
    /// </summary>
    public DbfVersion Version => _header.Version;

    /// <summary>
    /// Gets the language/code-page marker stored in the file header.
    /// </summary>
    public DbfLanguage Language => _header.Language;

    /// <summary>
    /// Gets the text encoding derived from <see cref="Language"/> and used for character fields.
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the decimal separator used when parsing and formatting numeric values.
    /// </summary>
    public char DecimalSeparator { get; }

    /// <summary>
    /// Gets the last-update date stored in the DBF header.
    /// </summary>
    public DateOnly LastUpdate => _header.LastUpdate;

    internal int HeaderLength => _header.HeaderLength;

    internal int RecordLength => _header.RecordLength;

    /// <summary>
    /// Gets the number of records tracked by the DBF header.
    /// </summary>
    /// <remarks>
    /// This count includes records marked as deleted in the on-disk delete-flag byte.
    /// </remarks>
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
    /// Gets the field descriptors that define the table schema.
    /// </summary>
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets the memo storage associated with this table, if available.
    /// </summary>
    public Memo? Memo { get; }

    /// <summary>
    /// Gets the Visual FoxPro DBC backlink area from the header.
    /// </summary>
    public DbcBacklink DbcBacklink { get; }

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Zero-based record index.</param>
    /// <returns>The record at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0 or greater than or equal to <see cref="RecordCount"/>.
    /// </exception>
    /// <remarks>
    /// This indexer is positional and does not skip records marked as deleted.
    /// </remarks>
    public DbfRecord this[int index] => GetRecord(index);

    private Dbf(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors, Memo? memo, DbcBacklink dbcBacklink)
    {
        _dbf = dbf;
        _header = header;
        Descriptors = descriptors;
        descriptors.EnsureFieldOffsets();

        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();
        DbcBacklink = dbcBacklink;

        Memo = memo;
    }

    /// <summary>
    /// Opens an existing dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to open.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    /// <remarks>
    /// This method attempts to open a sibling memo file with <c>.dbt</c> extension first, then <c>.fpt</c>.
    /// </remarks>
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

        var header = ReadHeader(dbf);
        var descriptors = ReadDescriptors(dbf, header.Version);
        var dbcBacklink = ReadDbcBacklink(dbf, header.Version, header.HeaderLength - DbcBacklink.Size);
        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Open(memo, header.Version) : null,
            dbcBacklink);
    }

    /// <summary>
    /// Creates a new dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="descriptors">The field descriptors that define the record structure.</param>
    /// <param name="version">The version of the dBASE database file.</param>
    /// <param name="language">The language of the dBASE database file.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    /// <remarks>
    /// A memo file is created automatically when the schema contains memo fields. FoxPro versions use
    /// <c>.fpt</c>; other versions use <c>.dbt</c>.
    /// </remarks>
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

    /// <summary>
    /// Creates a new dBASE database file using field descriptors inferred from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The record type used to derive the table schema.</typeparam>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="version">The version of the dBASE database file.</param>
    /// <param name="language">The language/code-page marker written to the DBF header.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    public static Dbf Create<T>(
        string fileName,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.Ansi)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var descriptors = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(properties.Length);
        foreach (var property in properties)
        {
            descriptors.Add(DbfFieldDescriptor.FromProperty(property, version));
        }

        return Create(fileName, descriptors.MoveToImmutable(), version, language);
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
        var dbcBacklink = version.IsFoxPro()
            ? new DbcBacklink(new byte[DbcBacklink.Size])
            : DbcBacklink.Empty;

        WriteHeader(dbf, in header);
        WriteDescriptors(dbf, header.Version, descriptors);
        WriteDbcBacklink(dbf, header.Version, header.HeaderLength - DbcBacklink.Size, dbcBacklink);

        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Create(memo, version) : null,
            dbcBacklink);
    }

    /// <summary>
    /// Saves this table to a new file path and writes memo data when present.
    /// </summary>
    /// <remarks>If the current state does not include a memo, only the main file is created. Otherwise, an
    /// additional memo file is created with an extension determined by the version.</remarks>
    /// <param name="fileName">The name of the file to which the current state will be saved.</param>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> is null, empty, or whitespace.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="dbf"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The DBF stream always receives a full file copy. Memo data is only copied when this instance has a
    /// memo file and <paramref name="memo"/> is provided.
    /// </remarks>
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
    /// Flushes pending changes and releases all file resources held by this instance.
    /// </summary>
    public void Dispose()
    {
        Flush();
        Memo?.Dispose();
        _dbf.Dispose();
    }

    /// <summary>
    /// Writes header updates and flushes DBF and memo streams.
    /// </summary>
    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeader(_dbf, in _header);
            WriteDescriptors(_dbf, _header.Version, Descriptors);
            WriteDbcBacklink(_dbf, _header.Version, _header.HeaderLength - DbcBacklink.Size, DbcBacklink);
        }

        Memo?.Flush();
        _dbf.Flush();
    }

    internal static DbfHeader ReadHeader(Stream dbf)
    {
        dbf.Position = 0;
        var version = (DbfVersion)dbf.ReadByte();
        dbf.Position = 0;

        if (!Enum.IsDefined(version))
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'");
        }

        return version is DbfVersion.DBase02
            ? dbf.Read<DbfHeader02>()
            : dbf.Read<DbfHeader>();
    }

    internal static ImmutableArray<DbfFieldDescriptor> ReadDescriptors(Stream dbf, DbfVersion version)
    {
        var builder = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(initialCapacity: 8);

        if (version is DbfVersion.DBase02)
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

        return builder.ToImmutable();

        static bool TryReadDescriptor<T>(Stream dbf, out T descriptor) where T : unmanaged =>
            dbf.TryRead(out descriptor) && Unsafe.As<T, byte>(ref descriptor) is not 0x0D;
    }

    internal static DbcBacklink ReadDbcBacklink(Stream dbf, DbfVersion version, int offset)
    {
        if (!version.IsFoxPro())
            return DbcBacklink.Empty;

        dbf.Position = offset;
        var rawBackLink = new byte[DbcBacklink.Size];
        dbf.ReadExactly(rawBackLink);
        return new DbcBacklink(rawBackLink);
    }

    internal static void WriteHeader(Stream dbf, in DbfHeader header)
    {
        if (!Enum.IsDefined(header.Version))
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        dbf.Position = 0;

        if (header.Version is DbfVersion.DBase02)
            dbf.Write((DbfHeader02)header);
        else
            dbf.Write(header);
    }

    internal static void WriteDescriptors(Stream dbf, DbfVersion version, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        if (version is DbfVersion.DBase02)
        {
            dbf.Position = DbfHeader02.Size;
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
            dbf.Position = DbfHeader.Size;
            foreach (var descriptor in descriptors)
            {
                dbf.Write(descriptor);
            }

            dbf.WriteByte(0x0D);
        }
    }

    internal static void WriteDbcBacklink(Stream dbf, DbfVersion version, int offset, DbcBacklink dbcBacklink)
    {
        if (!version.IsFoxPro())
            return;

        dbf.Position = offset;
        dbf.Write(dbcBacklink.Content.Span);
    }

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The <see cref="DbfRecord"/> at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0 or greater than or equal to <see cref="RecordCount"/>.
    /// </exception>
    /// <remarks>
    /// This method returns records by physical position and does not skip deleted rows.
    /// </remarks>
    public DbfRecord GetRecord(int index) => ReadRecord<DbfRecord>(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the record at the specified <paramref name="index"/>.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="index">The zero-based index of the record to get.</param>
    /// <returns>The deserialized record value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0 or greater than or equal to <see cref="RecordCount"/>.
    /// </exception>
    /// <remarks>
    /// The delete-flag byte is not projected to <typeparamref name="T"/> and this method does not expose
    /// whether a row is marked as deleted.
    /// </remarks>
    public T GetRecord<T>(int index) => ReadRecord<T>(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Enumerates all records in the database.
    /// </summary>
    /// <returns>A lazy sequence that yields records in file order.</returns>
    /// <remarks>
    /// Enumeration includes rows marked as deleted.
    /// </remarks>
    public IEnumerable<DbfRecord> EnumerateRecords() => EnumerateRecords<DbfRecord>();

    /// <summary>
    /// Enumerates all records in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <returns>A lazy sequence that yields records in file order.</returns>
    /// <remarks>
    /// Enumeration includes rows marked as deleted. The delete-flag byte is not projected to
    /// <typeparamref name="T"/>.
    /// </remarks>
    public IEnumerable<T> EnumerateRecords<T>()
    {
        var index = 0;
        while (ReadRecord<T>(index++, out var record))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Appends a record to the end of the table.
    /// </summary>
    /// <param name="record">The record to add.</param>
    /// <remarks>
    /// Records are written with a valid (not deleted) status marker.
    /// </remarks>
    public void Add(DbfRecord record) => WriteRecord(RecordCount, record);

    /// <summary>
    /// Appends a typed record value to the end of the table.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="record">The record to add.</param>
    /// <remarks>
    /// Records are written with a valid (not deleted) status marker.
    /// </remarks>
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
