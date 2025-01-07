using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext;
using DotNext.Buffers;

namespace DBase;

public sealed class Dbt : IMemo
{
    private static readonly byte[] s_recordTerminator = [0x1A, 0x1A];

    private readonly Stream _dbt;
    private DbtHeader _header;
    private bool _dirty;

    public ref readonly DbtHeader Header => ref _header;

    private int FirstIndex => Math.Max(1, DbtHeader.HeaderLengthInDisk / _header.BlockLength);

    public int NextIndex => _header.NextIndex;

    public ushort BlockLength => _header.BlockLength;

    public MemoRecord this[int index] { get => Get(index); }

    private Dbt(Stream dbt, DbtHeader header)
    {
        _dbt = dbt;
        _header = header;
    }

    public static Dbt Open(string fileName) =>
        Open(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite));

    public static Dbt Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Unsafe.SkipInit(out DbtHeader header);
        stream.Position = 0;
        stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));

        return new Dbt(stream, header);
    }

    public static Dbt Create(string fileName, ushort blockLength = DbtHeader.HeaderLengthInDisk, DbfVersion version = DbfVersion.DBase83) =>
        Create(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), blockLength, version);

    public static Dbt Create(Stream stream, ushort blockLength = DbtHeader.HeaderLengthInDisk, DbfVersion version = DbfVersion.DBase83)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new DbtHeader
        {
            BlockLength = blockLength,
            NextIndex = Math.Max(1, DbtHeader.HeaderLengthInDisk / blockLength),
            Version = version is DbfVersion.DBase03 or DbfVersion.DBase83 ? (byte)version : (byte)0
        };

        var dbt = new Dbt(stream, header);

        dbt.WriteHeader();

        return dbt;
    }

    public void Dispose()
    {
        Flush();
        _dbt.Dispose();
    }

    public void Flush()
    {
        if (_dirty)
        {
            _dirty = false;
            WriteHeader();
        }
        _dbt.Flush();
    }

    public void Add(MemoRecord record) =>
        Set(_header.NextIndex, record);

    internal void WriteHeader()
    {
        _dbt.Position = 0;
        _dbt.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _header, 1)));
    }

    internal void SetStreamPositionForIndex(int index) =>
        _dbt.Position = index * _header.BlockLength;

    internal bool Get(ref int index, out MemoRecord record)
    {
        record = default;

        if (index >= _header.NextIndex)
        {
            return false;
        }

        SetStreamPositionForIndex(index);

        using var writer = new BufferWriterSlim<byte>(_header.BlockLength);

        while (true)
        {
            var buffer = writer.GetSpan(_header.BlockLength);
            var bytesRead = _dbt.ReadAtLeast(buffer[.._header.BlockLength], _header.BlockLength, throwOnEndOfStream: false);
            buffer = buffer[..bytesRead];
            if (buffer.IndexOf(s_recordTerminator) is var end and >= 0)
                buffer = buffer[..end];
            ++index;

            writer.Advance(buffer.Length);

            // The buffer is smaller than block length, we reached
            // the end of the record or the end of the file.
            if (buffer.Length < _header.BlockLength)
            {
                break;
            }
        }

        record = new MemoRecord(MemoRecordType.Memo, writer.WrittenSpan.ToArray());

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

        foreach (var record in records)
        {
            _dbt.Write(record.Span);
            _dbt.Write(s_recordTerminator);

            var length = record.Length + 2;
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
