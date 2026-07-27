using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    /// Gets the resolved text encoding used for character, varchar, and memo text fields.
    /// </summary>
    /// <remarks>
    /// When opened from a path, the encoding is resolved from explicit open options, then a sibling
    /// <c>.cpg</c> file, then the DBF language-driver marker, then the configured fallback encoding.
    /// </remarks>
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

    private Dbf(
        Stream dbf,
        in DbfHeader header,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Memo? memo,
        DbcBacklink dbcBacklink,
        Encoding encoding)
    {
        _dbf = dbf;
        _header = header;
        Descriptors = descriptors;
        descriptors.EnsureFieldOffsets();

        Encoding = encoding;
        DecimalSeparator = DbfLanguageExtensions.TryGetDecimalSeparator(_header.Language) ??
            DbfLanguage.Ansi.GetDecimalSeparator();
        DbcBacklink = dbcBacklink;

        Memo = memo;
    }

    /// <summary>
    /// Opens an existing dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to open.</param>
    /// <param name="options">Optional settings that control text encoding resolution.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    /// <remarks>
    /// This method opens a sibling memo file when the DBF version maps to a supported memo format and the
    /// expected memo extension exists. Text encoding precedence is explicit <paramref name="options"/>,
    /// sibling <c>.cpg</c> file, DBF language-driver marker, then <see cref="DbfOpenOptions.FallbackEncoding"/>.
    /// </remarks>
    public static Dbf Open(string fileName, DbfOpenOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        options ??= new DbfOpenOptions();

        FileStream? dbf = null;
        FileStream? memo = null;

        try
        {
            dbf = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            var header = ReadHeader(dbf);
            var encoding = options.Encoding ??
                DbfEncoding.TryReadCodePageFile(fileName) ??
                DbfEncoding.Resolve(header.Language, options.FallbackEncoding);
            dbf.Position = 0;

            if (header.Version.HasSupportedMemo())
            {
                var memoName = Path.ChangeExtension(fileName, header.Version.GetMemoFileExtension());
                memo = File.Exists(memoName) ? new FileStream(memoName, FileMode.Open, FileAccess.ReadWrite) : null;
            }

            var result = Open(dbf, memo, encoding);
            dbf = null;
            memo = null;
            return result;
        }
        catch
        {
            memo?.Dispose();
            dbf?.Dispose();
            throw;
        }
    }

    internal static Dbf Open(Stream dbf, Stream? memo, Encoding? encoding)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var header = ReadHeader(dbf);
        ValidateHeaderGeometry(in header);
        EnsureStreamContains(dbf, header.HeaderLength, "DBF header");
        var descriptors = ReadDescriptors(dbf, in header);
        ValidateRecordGeometry(in header, descriptors);
        EnsureStreamContains(dbf, GetMinimumTableLength(in header), "DBF records");
        var dbcBacklink = ReadDbcBacklink(dbf, header.Version, header.HeaderLength - DbcBacklink.Size);
        return new Dbf(
            dbf,
            in header,
            descriptors,
            memo is not null ? Memo.Open(memo, header.Version) : null,
            dbcBacklink,
            encoding ?? DbfEncoding.Resolve(header.Language, DbfEncoding.DefaultFallbackEncoding));
    }

    /// <summary>
    /// Creates a new dBASE database file.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="descriptors">The field descriptors that define the record structure.</param>
    /// <param name="options">Optional settings that control the DBF version, language marker, and text encoding.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    /// <remarks>
    /// A memo file is created automatically when the schema contains memo-backed fields. The memo extension
    /// and on-disk format are selected from the resolved DBF version.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// The schema contains memo-backed fields, but <see cref="DbfCreateOptions.Version"/> does not have a
    /// supported memo file format; or <see cref="DbfCreateOptions.Version"/> is
    /// <see cref="DbfVersion.Unspecified"/> and no supported version can be inferred from
    /// <paramref name="descriptors"/>.
    /// </exception>
    public static Dbf Create(
        string fileName,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        DbfCreateOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        options ??= new DbfCreateOptions();

        var version = options.Version;
        var language = options.Language;
        version = descriptors.ResolveVersion(version);
        var memoFileName = descriptors.HasMemoFields()
            ? Path.ChangeExtension(fileName, version.GetMemoFileExtension())
            : null;

        if (memoFileName is not null && File.Exists(memoFileName))
        {
            throw new IOException($"The file '{memoFileName}' already exists.");
        }

        FileStream? dbf = null;
        FileStream? memo = null;

        try
        {
            dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
            if (memoFileName is not null)
            {
                memo = new FileStream(memoFileName, FileMode.CreateNew, FileAccess.ReadWrite);
            }

            var result = Create(dbf, descriptors, memo, options);
            dbf = null;
            memo = null;
            return result;
        }
        catch
        {
            memo?.Dispose();
            dbf?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a new dBASE database file using field descriptors inferred from <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The record type used to derive the table schema.</typeparam>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="options">Optional settings that control the DBF version, language marker, and text encoding.</param>
    /// <returns>An initialized <see cref="Dbf"/> instance.</returns>
    public static Dbf Create<T>(
        string fileName,
        DbfCreateOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        options ??= new DbfCreateOptions();

        var properties = DbfTypeProperties.GetMappedProperties(typeof(T));
        var descriptors = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(properties.Length);
        foreach (var property in properties)
        {
            descriptors.Add(DbfFieldDescriptor.FromProperty(property, options.Version));
        }

        var resolvedDescriptors = descriptors.MoveToImmutable();
        resolvedDescriptors.EnsureFieldOffsets();
        _ = resolvedDescriptors.GetSerializer<T>();

        return Create(fileName, resolvedDescriptors, options);
    }

    internal static Dbf Create(
        Stream dbf,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Stream? memo = null,
        DbfCreateOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(dbf);
        options ??= new DbfCreateOptions();

        var version = options.Version;
        var language = options.Language;
        version = descriptors.ResolveVersion(version);

        if (descriptors.HasMemoFields())
        {
            if (memo is null)
            {
                throw new ArgumentException("A memo stream is required when descriptors contain memo-backed fields.", nameof(memo));
            }

            _ = version.GetMemoFormat();
        }
        else if (memo is not null)
        {
            _ = version.GetMemoFormat();
        }

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
            dbcBacklink,
            options.Encoding ?? DbfEncoding.Resolve(language, DbfEncoding.DefaultFallbackEncoding));
    }

    /// <summary>
    /// Saves this table to a new file path and writes memo data when present.
    /// </summary>
    /// <remarks>If the current state does not include a memo, only the main file is created. Otherwise, an
    /// additional memo file is created with the extension mapped from the DBF version.</remarks>
    /// <param name="fileName">The name of the file to which the current state will be saved.</param>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="NotSupportedException">The current version does not have a supported memo file format.</exception>
    public void SaveAs(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var memoFileName = Memo is not null
            ? Path.ChangeExtension(fileName, Version.GetMemoFileExtension())
            : null;

        if (memoFileName is not null && File.Exists(memoFileName))
        {
            throw new IOException($"The file '{memoFileName}' already exists.");
        }

        using var dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        if (memoFileName is null)
        {
            WriteTo(dbf, null);
            return;
        }

        using var memo = new FileStream(memoFileName, FileMode.CreateNew, FileAccess.ReadWrite);
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

    private static void ValidateHeaderGeometry(in DbfHeader header)
    {
        if (header.Version is DbfVersion.DBase02)
        {
            if (header.HeaderLength != DbfHeader02.HeaderLengthInDisk)
            {
                throw new InvalidDataException($"Invalid dBASE II header length {header.HeaderLength}.");
            }
        }
        else
        {
            var minimumHeaderLength = DbfHeader.Size + 1;
            if (header.Version.IsFoxPro())
            {
                minimumHeaderLength += DbcBacklink.Size;
            }

            if (header.HeaderLength < minimumHeaderLength)
            {
                throw new InvalidDataException($"Invalid DBF header length {header.HeaderLength}.");
            }
        }

        if (header.RecordLength is 0)
        {
            throw new InvalidDataException("Invalid DBF record length 0.");
        }
    }

    private static void ValidateRecordGeometry(in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var expectedRecordLength = 1;
        foreach (var descriptor in descriptors)
        {
            expectedRecordLength += descriptor.Length;
        }

        if (header.RecordLength != expectedRecordLength)
        {
            throw new InvalidDataException(
                $"Invalid DBF record length {header.RecordLength}; expected {expectedRecordLength} bytes from the field descriptors.");
        }
    }

    private static void EnsureStreamContains(Stream dbf, long requiredLength, string description)
    {
        if (dbf.CanSeek && dbf.Length < requiredLength)
        {
            throw new EndOfStreamException($"The DBF stream ended before the declared {description} length of {requiredLength} bytes.");
        }
    }

    private static long GetMinimumTableLength(in DbfHeader header) =>
        header.HeaderLength + (long)header.RecordCount * header.RecordLength;

    private static int GetDescriptorStart(DbfVersion version) =>
        version is DbfVersion.DBase02 ? DbfHeader02.Size : DbfHeader.Size;

    private static int GetDescriptorSize(DbfVersion version) =>
        version is DbfVersion.DBase02 ? DbfFieldDescriptor02.Size : DbfFieldDescriptor.Size;

    private static int GetDescriptorLimit(in DbfHeader header) =>
        header.Version.IsFoxPro() ? header.HeaderLength - DbcBacklink.Size : header.HeaderLength;

    internal static DbfHeader ReadHeader(Stream dbf)
    {
        dbf.Position = 0;
        var versionByte = dbf.ReadByte();
        if (versionByte < 0)
        {
            throw new EndOfStreamException("The DBF stream ended before the file header could be read.");
        }

        var version = (DbfVersion)versionByte;
        dbf.Position = 0;

        if (version is DbfVersion.Unspecified || !Enum.IsDefined(version))
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'");
        }

        return version is DbfVersion.DBase02
            ? dbf.Read<DbfHeader02>()
            : dbf.Read<DbfHeader>();
    }

    internal static ImmutableArray<DbfFieldDescriptor> ReadDescriptors(Stream dbf, in DbfHeader header)
    {
        var builder = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(initialCapacity: 8);
        var descriptorStart = GetDescriptorStart(header.Version);
        var descriptorSize = GetDescriptorSize(header.Version);
        var descriptorLimit = GetDescriptorLimit(in header);
        dbf.Position = descriptorStart;

        while (dbf.Position < descriptorLimit)
        {
            var marker = dbf.ReadByte();
            if (marker < 0)
            {
                throw new EndOfStreamException("The DBF stream ended before the field descriptor terminator could be read.");
            }

            if (marker is 0x0D)
            {
                if (header.Version is not DbfVersion.DBase02 && dbf.Position != descriptorLimit)
                {
                    throw new InvalidDataException("Invalid DBF header length for the field descriptor list.");
                }

                return builder.ToImmutable();
            }

            --dbf.Position;
            if (descriptorLimit - dbf.Position < descriptorSize)
            {
                throw new InvalidDataException("A DBF field descriptor extends beyond the declared header length.");
            }

            if (header.Version is DbfVersion.DBase02)
            {
                builder.Add(dbf.Read<DbfFieldDescriptor02>());
            }
            else
            {
                builder.Add(dbf.Read<DbfFieldDescriptor>());
            }
        }

        throw new InvalidDataException("Missing DBF field descriptor terminator.");
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

        record = Descriptors.GetSerializer<T>().Deserialize(
            buffer.Span,
            CreateSerializationContext<T>(DbfSerializationOperation.Read, recordIndex));

        return true;
    }

    internal void WriteRecord<T>(int index, T record)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, RecordCount);

        SetStreamPositionForRecord(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        Descriptors.GetSerializer<T>().Serialize(
            buffer.Span,
            record,
            CreateSerializationContext<T>(DbfSerializationOperation.Write, index));
        _dbf.Write(buffer.Span);

        RecordCount = Math.Max(RecordCount, index + 1);
    }

    private DbfSerializationContext CreateSerializationContext<T>(DbfSerializationOperation operation, int recordIndex) =>
        new(Encoding, Memo, DecimalSeparator, operation, recordIndex, Version, Language, typeof(T));
}
