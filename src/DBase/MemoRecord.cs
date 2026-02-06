namespace DBase;

/// <summary>
/// Represents a single memo payload and its logical type.
/// </summary>
/// <param name="Type">Logical memo-record type marker.</param>
/// <param name="Data">Raw memo payload bytes (without format-specific block headers).</param>
public readonly record struct MemoRecord(MemoRecordType Type, ReadOnlyMemory<byte> Data)
{
    /// <summary>
    /// Gets the payload length in bytes.
    /// </summary>
    public int Length => Data.Length;

    /// <summary>
    /// Gets the payload data as a span.
    /// </summary>
    public ReadOnlySpan<byte> Span => Data.Span;
}
