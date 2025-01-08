﻿using System.Buffers.Binary;
using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DBase.Internal;
using DotNext;
using DotNext.Buffers;

namespace DBase;

public sealed class Memo : IDisposable, IEnumerable<MemoRecord>
{
    internal const ushort HeaderLengthInDisk = 512;

    private static readonly byte[] s_recordTerminatorV3 = [0x1A, 0x1A];

    private delegate bool GetDelegate(ref int index, out MemoRecord record);
    private delegate void SetDelegate(int index, params ReadOnlySpan<MemoRecord> records);

    private readonly Stream _memo;
    private readonly GetDelegate _get;
    private readonly SetDelegate _set;
    private readonly DbfVersion _version;
    private bool _dirty;

    internal int FirstIndex => GetBlockCount(HeaderLengthInDisk, BlockLength);

    public int NextIndex { get; private set; }

    public ushort BlockLength { get; }

    public MemoRecord this[int index] { get => Get(index); }

    private Memo(Stream memo, DbfVersion version)
    {
        _memo = memo;
        (NextIndex, BlockLength) = ReadHeaderInfo(memo, version);
        _version = version;
        (_get, _set) = version switch
        {
            DbfVersion.DBase83 => ((GetDelegate)Get83, (SetDelegate)Set83),
            DbfVersion.DBase8B => (Get8B, Set8B),
            DbfVersion.VisualFoxPro => (GetFP, SetFP),
            DbfVersion.VisualFoxProWithAutoIncrement => (GetFP, SetFP),
            DbfVersion.VisualFoxProWithVarchar => (GetFP, SetFP),
            DbfVersion.FoxPro2WithMemo => (GetFP, SetFP),
            _ => throw new NotSupportedException($"Unsupported DBF version '{(byte)version}'")
        };
    }

    public static Memo Open(string fileName, DbfVersion version) =>
        Open(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite), version);

    public static Memo Open(Stream stream, DbfVersion version)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new Memo(stream, version);
    }

    public static Memo Create(string fileName, DbfVersion version, ushort blockLength = HeaderLengthInDisk) =>
        Create(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), version, blockLength);

    public static Memo Create(Stream stream, DbfVersion version, ushort blockLength = HeaderLengthInDisk)
    {
        ArgumentNullException.ThrowIfNull(stream);

        WriteHeaderInfo(stream, version, GetBlockCount(HeaderLengthInDisk, blockLength), blockLength);

        return new Memo(stream, version);
    }

    internal int GetBlockCount(MemoRecord record)
    {
        var lengthInDisk = _version is DbfVersion.DBase83 ? record.Length : record.Length + 8;
        return GetBlockCount(lengthInDisk, BlockLength);
    }

    private static int GetBlockCount(long length, int blockLength) =>
        (int)((length + blockLength - 1) / blockLength);

    private static (int nextIndex, ushort blockLength) ReadHeaderInfo(Stream stream, DbfVersion version)
    {
        switch (version)
        {
            case DbfVersion.DBase83:
            case DbfVersion.DBase8B:
                {
                    Unsafe.SkipInit(out DbtHeader header);
                    stream.Position = 0;
                    stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
                    return (header.NextIndex, header.BlockLength);
                }
            case DbfVersion.VisualFoxPro:
            case DbfVersion.VisualFoxProWithAutoIncrement:
            case DbfVersion.VisualFoxProWithVarchar:
            case DbfVersion.FoxPro2WithMemo:
                {
                    Unsafe.SkipInit(out FptHeader header);
                    stream.Position = 0;
                    stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
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
                    var header = new DbtHeader
                    {
                        NextIndex = nextIndex,
                        BlockLength = blockLength
                    };
                    stream.Position = 0;
                    stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
                }
                break;

            case DbfVersion.VisualFoxPro:
            case DbfVersion.VisualFoxProWithAutoIncrement:
            case DbfVersion.VisualFoxProWithVarchar:
            case DbfVersion.FoxPro2WithMemo:
                {
                    var header = new FptHeader
                    {
                        NextIndex = nextIndex,
                        BlockLength = blockLength
                    };
                    stream.Position = 0;
                    stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
                }
                break;

            default:
                throw new NotSupportedException($"Unsupported DBF version '{(byte)version}'");
        }
    }

    public void Dispose()
    {
        Flush();
        _memo.Dispose();
    }

    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeaderInfo(_memo, _version, NextIndex, BlockLength);
        }
        _memo.Flush();
    }

    public void Add(MemoRecord record) => Set83(NextIndex, record);

    internal void SetStreamPositionForIndex(int index) => _memo.Position = index * BlockLength;

    internal bool Get83(ref int index, out MemoRecord record)
    {
        record = default;

        if (index >= NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        using var writer = new BufferWriterSlim<byte>(BlockLength);

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

        record = new MemoRecord(MemoRecordType.Memo, writer.WrittenSpan.ToArray());

        return true;
    }

    internal bool Get8B(ref int index, out MemoRecord record)
    {
        record = default;

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

        var data = new byte[length];

        if (_memo.ReadAtLeast(data, length, throwOnEndOfStream: false) != length)
            return false;
        index += GetBlockCount(length + 8, BlockLength);

        record = new MemoRecord(MemoRecordType.Memo, data);

        return true;
    }

    internal bool GetFP(ref int index, out MemoRecord record)
    {
        record = default;

        if (index >= NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        Span<byte> i32 = stackalloc byte[4];
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var type = (MemoRecordType)BinaryPrimitives.ReadInt32BigEndian(i32);
        if (!Enum.IsDefined(type))
            return false;
        if (_memo.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var length = BinaryPrimitives.ReadUInt32BigEndian(i32);

        var data = new byte[length];
        if (_memo.ReadAtLeast(data, data.Length, throwOnEndOfStream: false) != data.Length)
            return false;
        index += GetBlockCount(length + 8, BlockLength);

        record = new MemoRecord(type, data);

        return true;
    }

    internal MemoRecord Get(int index) =>
        _get(ref index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    internal void Set83(int index, params ReadOnlySpan<MemoRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        if (index != NextIndex)
        {
            // TODO: Support random access?
            throw new NotSupportedException("Random access is not supported");
        }

        SetStreamPositionForIndex(index);

        foreach (var record in records)
        {
            _memo.Write(record.Span);
            _memo.Write(s_recordTerminatorV3);

            var length = record.Length + 2;
            index += 1 + length / BlockLength;
        }

        if (index > NextIndex)
        {
            _dirty = true;
            NextIndex = Math.Max(NextIndex, index + 1);
        }
    }

    internal void Set8B(int index, params ReadOnlySpan<MemoRecord> records) =>
        throw new NotImplementedException();

    internal void SetFP(int index, params ReadOnlySpan<MemoRecord> records) =>
        throw new NotImplementedException();

    public IEnumerator<MemoRecord> GetEnumerator()
    {
        var index = FirstIndex;
        while (_get(ref index, out var record))
        {
            yield return record;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}