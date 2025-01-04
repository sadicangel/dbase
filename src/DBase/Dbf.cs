using System.Collections;
using System.Collections.Immutable;
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

    public Dbt? Dbt { get; }
    public Fpt? Fpt { get; }

    public int Count
    {
        get => (int)_header.RecordCount;
        private set
        {
            _header = _header with
            {
                RecordCount = (uint)value,
                LastUpdate = DateOnly.FromDateTime(DateTime.Now)
            };
            _dirty = true;
        }
    }

    public DbfRecord this[int index] { get => Get(index); }

    private Dbf(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors, Dbt? dbt, Fpt? fpt)
    {
        _dbf = dbf;
        _header = header;
        Descriptors = descriptors;

        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();

        Dbt = dbt;
        Fpt = fpt;
    }

    public static Dbf Open(string fileName)
    {
        var dbf = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
        var dbtName = Path.ChangeExtension(fileName, "dbt");
        var dbt = File.Exists(dbtName) ? new FileStream(dbtName, FileMode.Open, FileAccess.ReadWrite) : null;
        var fptName = Path.ChangeExtension(fileName, "fpt");
        var fpt = dbt is null && File.Exists(fptName) ? new FileStream(fptName, FileMode.Open, FileAccess.ReadWrite) : null;
        return Open(dbf, dbt, fpt);
    }

    public static Dbf Open(Stream dbf, Stream? dbt = null, Stream? fpt = null)
    {
        ArgumentNullException.ThrowIfNull(dbf);

        var header = DbfHelper.ReadHeader(dbf);
        var descriptors = DbfHelper.ReadDescriptors(dbf, in header);
        return new Dbf(
            dbf,
            in header,
            descriptors,
            dbt is not null ? Dbt.Open(dbt) : null,
            fpt is not null ? Fpt.Open(fpt) : null);
    }

    public static Dbf Create(
        string fileName,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        DbfVersion version = DbfVersion.DBase03,
        DbfLanguage language = DbfLanguage.ANSI)
    {
        var dbf = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        var dbt = default(FileStream);
        var fpt = default(FileStream);
        if (DbfHelper.GetTableFlagsFromDescriptors(descriptors).HasFlag(DbfTableFlags.HasMemoField))
        {
            if (version.IsFoxPro())
            {
                fpt = new FileStream(Path.ChangeExtension(fileName, "fpt"), FileMode.CreateNew, FileAccess.ReadWrite);
            }
            else
            {
                dbt = new FileStream(Path.ChangeExtension(fileName, "dbt"), FileMode.CreateNew, FileAccess.ReadWrite);
            }
        }
        return Create(dbf, descriptors, dbt, fpt, version, language);
    }

    public static Dbf Create(
        Stream dbf,
        ImmutableArray<DbfFieldDescriptor> descriptors,
        Stream? dbt = null,
        Stream? fpt = null,
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
            TableFlags = DbfHelper.GetTableFlagsFromDescriptors(descriptors),
            Version = version,
        };

        DbfHelper.WriteHeader(dbf, in header, descriptors);

        return new Dbf(
            dbf,
            in header,
            descriptors,
            dbt is not null ? Dbt.Create(dbt) : null,
            fpt is not null ? Fpt.Create(fpt) : null);
    }

    public void Dispose()
    {
        Flush();
        Dbt?.Dispose();
        _dbf.Dispose();
    }

    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            DbfHelper.WriteHeader(_dbf, in _header, Descriptors);
        }
        _dbf.Flush();
    }

    internal long SetStreamPositionForIndex(int index) =>
        _dbf.Position = _header.HeaderLength + index * _header.RecordLength;

    internal bool Get(int index, out DbfRecord record)
    {
        record = default;

        if (index >= Count)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        var bytesRead = _dbf.ReadAtLeast(buffer.Span, _header.RecordLength, throwOnEndOfStream: false);
        if (bytesRead != _header.RecordLength)
        {
            return false;
        }

        record = DbfHelper.ReadRecord(buffer.Span, Descriptors, Encoding, DecimalSeparator);

        return true;
    }

    internal DbfRecord Get(int index) =>
        Get(index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    internal void Set(int index, params ReadOnlySpan<DbfRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        SetStreamPositionForIndex(index);

        using var buffer = _header.RecordLength < StackallocThreshold
            ? new SpanOwner<byte>(stackalloc byte[_header.RecordLength])
            : new SpanOwner<byte>(_header.RecordLength);

        foreach (var record in records)
        {
            DbfHelper.WriteRecord(record, Descriptors, Encoding, DecimalSeparator, buffer.Span);
            _dbf.Write(buffer.Span);
        }

        Count += records.Length;
    }

    public IEnumerator<DbfRecord> GetEnumerator()
    {
        var index = 0;
        while (Get(index++, out var record))
        {
            yield return record;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
