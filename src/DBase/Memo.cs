using System.Buffers.Binary;
using System.Collections;
using DBase.Interop;
using DotNext.Buffers;

namespace DBase;

/// <summary>
/// Represents a DBT/FPT memo file associated with a DBF table.
/// </summary>
/// <remarks>
/// Memo files are block-addressed. DBF records typically store a block index that points to data in this
/// file. The concrete on-disk memo format depends on <see cref="DbfVersion"/>.
/// </remarks>
public sealed class Memo : IDisposable, IEnumerable<MemoRecord>
{
    internal const ushort HeaderLengthInDisk = 512;

    private static readonly byte[] s_recordTerminatorV3 = [0x1A, 0x1A];

    private delegate bool GetDelegate(ref int index, out MemoRecordType type, ref BufferWriterSlim<byte> writer);

    private delegate void SetDelegate(int index, MemoRecordType type, ReadOnlySpan<byte> data);

    private delegate int LenDelegate(ReadOnlySpan<byte> data);

    private readonly Stream _memo;
    private readonly GetDelegate _get;
    private readonly SetDelegate _set;
    private readonly LenDelegate _len;
    private readonly DbfVersion _version;
    private bool _dirty;

    internal int FirstIndex => GetBlockCount(HeaderLengthInDisk, BlockLength);

    internal int NextIndex { get; private set; }

    internal ushort BlockLength { get; }

    /// <summary>
    /// Gets the memo record stored at the specified block index.
    /// </summary>
    /// <param name="index">Block index of the memo record.</param>
    /// <returns>The memo record at <paramref name="index"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not reference a readable record.</exception>
    /// <remarks>
    /// The index is a physical block index from DBF memo pointer fields, not an ordinal position in the
    /// enumeration returned by <see cref="GetEnumerator"/>.
    /// </remarks>
    public MemoRecord this[int index] { get => Get(index); }

    private Memo(Stream memo, DbfVersion version)
    {
        _memo = memo;
        (NextIndex, BlockLength) = ReadHeaderInfo(memo, version);
        _version = version;
        (_get, _set, _len) = version switch
        {
            DbfVersion.DBase83 => ((GetDelegate)Get83, (SetDelegate)Set83, (LenDelegate)Len83),
            DbfVersion.DBase8B => (Get8B, Set8B, Len8B),
            DbfVersion.VisualFoxPro => (GetFP, SetFP, LenFP),
            DbfVersion.VisualFoxProWithAutoIncrement => (GetFP, SetFP, LenFP),
            DbfVersion.VisualFoxProWithVarchar => (GetFP, SetFP, LenFP),
            DbfVersion.FoxPro2WithMemo => (GetFP, SetFP, LenFP),
            _ => throw new NotSupportedException($"Unsupported DBF version '{(byte)version}'")
        };
    }

    /// <summary>
    /// Opens an existing memo file.
    /// </summary>
    /// <param name="fileName">The name of the file to open.</param>
    /// <param name="version">DBF version that determines memo header and block encoding rules.</param>
    /// <returns>An opened <see cref="Memo"/> instance.</returns>
    /// <remarks>
    /// Header decoding and per-record framing are selected from <paramref name="version"/>.
    /// </remarks>
    public static Memo Open(string fileName, DbfVersion version) =>
        Open(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite), version);

    internal static Memo Open(Stream stream, DbfVersion version)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new Memo(stream, version);
    }

    /// <summary>
    /// Creates a new memo file.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="version">DBF version that determines memo header and block encoding rules.</param>
    /// <param name="blockLength">Size of each memo block, in bytes.</param>
    /// <returns>A created <see cref="Memo"/> instance.</returns>
    /// <remarks>
    /// The file starts with a 512-byte header region and initializes the next writable block index from
    /// the configured block size.
    /// </remarks>
    public static Memo Create(string fileName, DbfVersion version, ushort blockLength = HeaderLengthInDisk) =>
        Create(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), version, blockLength);

    internal static Memo Create(Stream stream, DbfVersion version, ushort blockLength = HeaderLengthInDisk)
    {
        ArgumentNullException.ThrowIfNull(stream);

        WriteHeaderInfo(stream, version, GetBlockCount(HeaderLengthInDisk, blockLength), blockLength);

        return new Memo(stream, version);
    }

    /// <summary>
    /// Saves this memo content to a new file path.
    /// </summary>
    /// <param name="fileName">The name of the file to which the data will be saved.</param>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> is null, empty, or whitespace.</exception>
    public void Save(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        using var memo = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        WriteTo(memo);
    }

    /// <summary>
    /// Writes this memo content to the specified stream.
    /// </summary>
    /// <param name="stream">Destination stream.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method copies the memo file as-is, including header and any unused blocks.
    /// </remarks>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _memo.Position = 0;
        _memo.CopyTo(stream);
    }

    private static (int nextIndex, ushort blockLength) ReadHeaderInfo(Stream stream, DbfVersion version)
    {
        stream.Position = 0;
        switch (version)
        {
            case DbfVersion.DBase83:
            case DbfVersion.DBase8B:
                {
                    var header = stream.Read<DbtHeader>();
                    return (header.NextIndex, header.BlockLength);
                }
            case DbfVersion.VisualFoxPro:
            case DbfVersion.VisualFoxProWithAutoIncrement:
            case DbfVersion.VisualFoxProWithVarchar:
            case DbfVersion.FoxPro2WithMemo:
                {
                    var header = stream.Read<FptHeader>();
                    return (header.NextIndex, header.BlockLength);
                }
            default:
                throw new NotSupportedException($"Unsupported DBF version '{(byte)version}'");
        }
    }

    private static void WriteHeaderInfo(Stream stream, DbfVersion version, int nextIndex, ushort blockLength)
    {
        switch (version)
        {
            case DbfVersion.DBase83:
            case DbfVersion.DBase8B:
                {
                    stream.Position = 0;
                    stream.Write(
                        new DbtHeader
                        {
                            NextIndex = nextIndex,
                            BlockLength = blockLength
                        });
                }
                break;

            case DbfVersion.VisualFoxPro:
            case DbfVersion.VisualFoxProWithAutoIncrement:
            case DbfVersion.VisualFoxProWithVarchar:
            case DbfVersion.FoxPro2WithMemo:
                {
                    stream.Position = 0;
                    stream.Write(
                        new FptHeader
                        {
                            NextIndex = nextIndex,
                            BlockLength = blockLength
                        });
                }
                break;

            default:
                throw new NotSupportedException($"Unsupported DBF version '{(byte)version}'");
        }
    }

    /// <summary>
    /// Flushes pending header changes and releases underlying stream resources.
    /// </summary>
    public void Dispose()
    {
        Flush();
        _memo.Dispose();
    }

    /// <summary>
    /// Writes pending header metadata and flushes the memo stream.
    /// </summary>
    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeaderInfo(_memo, _version, NextIndex, BlockLength);
        }

        _memo.Flush();
    }

    private static int GetBlockCount(long length, int blockLength) =>
        (int)((length + blockLength - 1) / blockLength);

    internal int GetRecordLengthInDisk(ReadOnlySpan<byte> record) => _len(record);

    internal int GetBlockCount(ReadOnlySpan<byte> record) => GetBlockCount(GetRecordLengthInDisk(record), BlockLength);

    private static int Len83(ReadOnlySpan<byte> record) => record.Length + 2;
    private static int Len8B(ReadOnlySpan<byte> record) => record.Length + 8;

    // ReSharper disable once InconsistentNaming
    private static int LenFP(ReadOnlySpan<byte> record) => record.Length + 8;

    /// <summary>
    /// Appends a memo record to the end of the file.
    /// </summary>
    /// <param name="record">Record payload and type to append.</param>
    /// <remarks>
    /// This implementation is append-only; random overwrite is not supported. Framing bytes written
    /// around payload data are format-dependent (DBT vs FPT).
    /// </remarks>
    public void Add(MemoRecord record) => _set(NextIndex, record.Type, record.Span);

    /// <summary>
    /// Appends a typed memo payload to the end of the file.
    /// </summary>
    /// <param name="type">Memo payload type marker.</param>
    /// <param name="data">Memo payload bytes.</param>
    /// <remarks>
    /// This implementation is append-only; random overwrite is not supported. Framing bytes written
    /// around payload data are format-dependent (DBT vs FPT).
    /// </remarks>
    public void Add(MemoRecordType type, ReadOnlySpan<byte> data) => _set(NextIndex, type, data);

    internal void SetStreamPositionForIndex(int index) => _memo.Position = index * BlockLength;

    internal bool Get83(ref int index, out MemoRecordType type, ref BufferWriterSlim<byte> writer)
    {
        type = MemoRecordType.Memo;

        if (index >= NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        while (true)
        {
            var buffer = writer.GetSpan(BlockLength);
            var bytesRead = _memo.ReadAtLeast(buffer[..BlockLength], BlockLength, throwOnEndOfStream: false);
            buffer = buffer[..bytesRead];
            if (buffer.IndexOf(s_recordTerminatorV3) is var end and >= 0)
                buffer = buffer[..end];
            ++index;

            writer.Advance(buffer.Length);

            // The buffer is smaller than block length, we reached
            // the end of the record or the end of the file.
            if (buffer.Length < BlockLength)
            {
                break;
            }
        }

        return true;
    }

    internal bool Get8B(ref int index, out MemoRecordType type, ref BufferWriterSlim<byte> writer)
    {
        type = MemoRecordType.Memo;

        if (index >= NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        Span<byte> i32 = stackalloc byte[4];
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        if (!i32.SequenceEqual((ReadOnlySpan<byte>)[0xFF, 0xFF, 0x08, 0x00]))
            return false;
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var length = BinaryPrimitives.ReadInt32LittleEndian(i32) - 8;

        var data = writer.GetSpan(length)[..length];

        if (_memo.ReadAtLeast(data, length, throwOnEndOfStream: false) != length)
            return false;

        writer.Advance(data.Length);

        index += GetBlockCount(data);

        return true;
    }

    // ReSharper disable once InconsistentNaming
    internal bool GetFP(ref int index, out MemoRecordType type, ref BufferWriterSlim<byte> writer)
    {
        type = default;

        if (index >= NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        Span<byte> i32 = stackalloc byte[4];
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        type = (MemoRecordType)BinaryPrimitives.ReadInt32BigEndian(i32);
        if (!Enum.IsDefined(type))
            return false;
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var length = (int)BinaryPrimitives.ReadUInt32BigEndian(i32);

        var data = writer.GetSpan(length)[..length];
        if (_memo.ReadAtLeast(data, length, throwOnEndOfStream: false) != length)
            return false;

        writer.Advance(data.Length);

        index += GetBlockCount(data);

        return true;
    }

    internal MemoRecord Get(int index)
    {
        var writer = new BufferWriterSlim<byte>(BlockLength);
        try
        {
            Get(index, out var type, ref writer);
            return new MemoRecord(type, writer.WrittenSpan.ToArray());
        }
        finally
        {
            writer.Dispose();
        }
    }

    internal void Get(int index, out MemoRecordType type, ref BufferWriterSlim<byte> writer)
    {
        if (!_get(ref index, out type, ref writer))
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal void Set83(int index, MemoRecordType type, ReadOnlySpan<byte> data)
    {
        if (index != NextIndex)
        {
            // TODO: Support random access?
            throw new NotSupportedException("Random access is not supported");
        }

        SetStreamPositionForIndex(index);
        _memo.Write(data);
        _memo.Write(s_recordTerminatorV3);
        index += GetBlockCount(data);

        _dirty = true;
        NextIndex = index;
    }

    internal void Set8B(int index, MemoRecordType type, ReadOnlySpan<byte> data)
    {
        if (index != NextIndex)
        {
            // TODO: Support random access?
            throw new NotSupportedException("Random access is not supported");
        }

        Span<byte> buffer = stackalloc byte[4];

        SetStreamPositionForIndex(index);

        _memo.Write([0xFF, 0xFF, 0x08, 0x00]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer, GetRecordLengthInDisk(data));
        _memo.Write(buffer);
        _memo.Write(data);
        index += GetBlockCount(data);

        _dirty = true;
        NextIndex = index;
    }

    // ReSharper disable once InconsistentNaming
    internal void SetFP(int index, MemoRecordType type, ReadOnlySpan<byte> data)
    {
        if (index != NextIndex)
        {
            // TODO: Support random access?
            throw new NotSupportedException("Random access is not supported");
        }

        Span<byte> buffer = stackalloc byte[4];

        SetStreamPositionForIndex(index);

        BinaryPrimitives.WriteInt32BigEndian(buffer, (int)type);
        _memo.Write(buffer);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)data.Length);
        _memo.Write(buffer);
        _memo.Write(data);
        index += GetBlockCount(data);

        _dirty = true;
        NextIndex = index;
    }

    /// <summary>
    /// Returns an enumerator that iterates readable memo records in block order.
    /// </summary>
    /// <returns>An enumerator over memo records.</returns>
    /// <remarks>
    /// Enumeration stops at the first unreadable record (for example, truncated data or invalid record
    /// framing) and does not attempt recovery.
    /// </remarks>
    public IEnumerator<MemoRecord> GetEnumerator()
    {
        var index = FirstIndex;
        while (GetOne(ref index, out var record))
        {
            yield return record;
        }

        yield break;

        bool GetOne(ref int offset, out MemoRecord record)
        {
            var writer = new BufferWriterSlim<byte>(BlockLength);
            try
            {
                if (_get(ref offset, out var type, ref writer))
                {
                    record = new MemoRecord(type, writer.WrittenSpan.ToArray());
                    return true;
                }

                record = default;
                return false;
            }
            finally
            {
                writer.Dispose();
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
