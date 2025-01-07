
namespace DBase;

public readonly record struct MemoRecord(MemoRecordType Type, ReadOnlyMemory<byte> Data)
{
    public int Length => Data.Length;
    public ReadOnlySpan<byte> Span => Data.Span;
}
