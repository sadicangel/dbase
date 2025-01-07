﻿using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase;

public sealed class Fpt : IMemo
{
    private static readonly byte[] s_recordTerminator = [0x1A, 0x1A];

    private readonly Stream _fpt;
    private FptHeader _header;
    private bool _dirty;

    public ref readonly FptHeader Header => ref _header;

    private int FirstIndex => Math.Max(1, FptHeader.HeaderLengthInDisk / _header.BlockLength);

    public int NextIndex => _header.NextIndex;

    public ushort BlockLength => _header.BlockLength;

    public MemoRecord this[int index] { get => Get(index); }

    private Fpt(Stream dbt, FptHeader header)
    {
        _fpt = dbt;
        _header = header;
    }

    public static Fpt Open(string fileName) =>
        Open(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite));

    public static Fpt Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Unsafe.SkipInit(out FptHeader header);
        stream.Position = 0;
        stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));

        return new Fpt(stream, header);
    }

    public static Fpt Create(string fileName, ushort blockLength = FptHeader.HeaderLengthInDisk) =>
        Create(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), blockLength);

    public static Fpt Create(Stream stream, ushort blockLength = FptHeader.HeaderLengthInDisk)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new FptHeader
        {
            BlockLength = blockLength,
            NextIndex = Math.Max(1, FptHeader.HeaderLengthInDisk / blockLength),
        };

        var dbt = new Fpt(stream, header);

        dbt.WriteHeader();

        return dbt;
    }

    public void Dispose()
    {
        Flush();
        _fpt.Dispose();
    }

    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeader();
        }
        _fpt.Flush();
    }

    public void Add(MemoRecord record) =>
        Set(_header.NextIndex, record);

    internal void WriteHeader()
    {
        _fpt.Position = 0;
        _fpt.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _header, 1)));
    }

    internal void SetStreamPositionForIndex(int index) =>
        _fpt.Position = index * _header.BlockLength;

    internal bool Get(ref int index, out MemoRecord record)
    {
        record = default;

        if (index >= _header.NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        Span<byte> i32 = stackalloc byte[4];
        if (_fpt.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var type = (MemoRecordType)BinaryPrimitives.ReadInt32BigEndian(i32);
        if (_fpt.ReadAtLeast(i32, 4, throwOnEndOfStream: false) != 4)
            return false;
        var length = BinaryPrimitives.ReadInt32BigEndian(i32);

        var data = new byte[length];
        if (_fpt.ReadAtLeast(data, data.Length, throwOnEndOfStream: false) != data.Length)
            return false;
        index += 1 + length / _header.BlockLength;

        record = new MemoRecord(type, data);

        return true;
    }

    internal MemoRecord Get(int index) =>
        Get(ref index, out var record) ? record : throw new ArgumentOutOfRangeException(nameof(index));

    internal void Set(int index, params ReadOnlySpan<MemoRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        if (index != _header.NextIndex)
        {
            // TODO: Support random access?
            throw new NotSupportedException("Random access is not supported");
        }

        SetStreamPositionForIndex(index);

        Span<byte> i32 = stackalloc byte[4];

        foreach (var record in records)
        {
            BinaryPrimitives.WriteInt32BigEndian(i32, (int)MemoRecordType.Memo);
            _fpt.Write(i32);
            BinaryPrimitives.WriteInt32BigEndian(i32, record.Length);
            _fpt.Write(i32);
            _fpt.Write(record.Span);
            _fpt.Write(s_recordTerminator);

            var length = 8 + record.Length;
            index += 1 + length / _header.BlockLength;
        }

        if (index > _header.NextIndex)
        {
            _dirty = true;
            _header = _header with { NextIndex = Math.Max(_header.NextIndex, index + 1) };
        }
    }

    public IEnumerator<MemoRecord> GetEnumerator()
    {
        var index = FirstIndex;
        while (Get(ref index, out var record))
        {
            yield return record;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
