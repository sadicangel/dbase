using System.Buffers.Binary;
using System.Collections;
using System.Collections.Immutable;
using DBase.Interop;
using DotNext;
using DotNext.Buffers;

namespace DBase;

/// <summary>
/// Represents a memo file associated with a dBASE file.
/// </summary>
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
    /// Gets the record at the specified index.
    /// </summary>
    /// <param name="index">The index of the record.</param>
    /// <returns>The <see cref="MemoRecord"/> at the specified index.</returns>
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
    /// <param name="version">The version of the dBASE file. The version affects how records are read and written.</param>
    /// <returns>The opened <see cref="Memo"/> file.</returns>
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
    /// <param name="fileName">The name of the file to open.</param>
    /// <param name="version">The version of the dBASE file. The version affects how records are read and written.</param>
    /// <param name="blockLength">The length of each block in the memo file.</param>
    /// <returns>The opened <see cref="Memo"/> file.</returns>
    public static Memo Create(string fileName, DbfVersion version, ushort blockLength = HeaderLengthInDisk) =>
        Create(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), version, blockLength);

    internal static Memo Create(Stream stream, DbfVersion version, ushort blockLength = HeaderLengthInDisk)
    {
        ArgumentNullException.ThrowIfNull(stream);

        WriteHeaderInfo(stream, version, GetBlockCount(HeaderLengthInDisk, blockLength), blockLength);

        return new Memo(stream, version);
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
                    stream.Write(new DbtHeader
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
                    stream.Write(new FptHeader
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
    /// Releases all resources used by the current instance of the <see cref="Memo"/> class.
    /// </summary>
    public void Dispose()
    {
        Flush();
        _memo.Dispose();
    }

    /// <summary>
    /// Flushes the memo file to disk.
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
    private static int LenFP(ReadOnlySpan<byte> record) => record.Length + 8;

    /// <summary>
    /// Adds a record to the memo file.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void Add(MemoRecord record) => _set(NextIndex, record.Type, record.Span);

    /// <summary>
    /// Adds a record to the memo file.
    /// </summary>
    /// <param name="type">The type of the record to add.</param>
    /// <param name="data">The data of the record to add.</param>
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
    /// Returns an enumerator that iterates through the memo records.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the memo records.</returns>
    public IEnumerator<MemoRecord> GetEnumerator()
    {
        var index = FirstIndex;
        while (Get(ref index, out var record))
        {
            yield return record;
        }

        bool Get(ref int index, out MemoRecord record)
        {
            var writer = new BufferWriterSlim<byte>(BlockLength);
            try
            {
                if (_get(ref index, out var type, ref writer))
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
