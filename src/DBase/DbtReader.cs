using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext;
using DotNext.Buffers;

namespace DBase;
public sealed class DbtReader : IDisposable
{
    private static readonly byte[] s_recordTerminator = [0x1A, 0x1A];

    private readonly Stream _dbt;
    private readonly DbtHeader _header;


    public ref readonly DbtHeader Header => ref _header;
    public int Index { get; private set; }

    public DbtReader(Stream dbt)
    {
        ArgumentNullException.ThrowIfNull(dbt);

        _dbt = dbt;
        _header = ReadHeader(dbt);

        Index = _header.BlockLength < DbtHeader.HeaderLengthInDisk
            ? (int)Math.Ceiling(DbtHeader.HeaderLengthInDisk / (double)_header.BlockLength)
            : 1;
    }

    public void Dispose() => _dbt.Dispose();

    private static DbtHeader ReadHeader(Stream stream)
    {
        stream.Position = 0;

        Unsafe.SkipInit(out DbtHeader header);
        stream.ReadAtLeast(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)), minimumBytes: DbtHeader.Size);
        return header;
    }

    public bool Read([MaybeNullWhen(false)] out DbtRecord record)
    {
        record = default;

        if (_dbt.Position >= _dbt.Length)
        {
            return false;
        }

        var index = Index;
        var blockLength = _header.BlockLength;
        _dbt.Position = index * blockLength;

        using var writer = new BufferWriterSlim<byte>(blockLength);

        while (true)
        {
            var buffer = writer.GetSpan(blockLength);
            var bytesRead = _dbt.ReadAtLeast(buffer[..blockLength], blockLength, throwOnEndOfStream: false);
            buffer = buffer[..bytesRead];
            if (buffer.IndexOf(s_recordTerminator) is var end and >= 0)
                buffer = buffer[..end];
            ++index;

            writer.Advance(buffer.Length);

            // We read less than block length, we reached
            // the end of the record or the end of the file.
            if (buffer.Length < blockLength)
            {
                break;
            }
        }

        record = new DbtRecord(writer.WrittenSpan);

        Index = index;

        return true;
    }

    public IEnumerable<DbtRecord> ReadRecords()
    {
        while (Read(out var record))
        {
            yield return record;
        }
    }
}
