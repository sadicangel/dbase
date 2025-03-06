
namespace DBase;

/// <summary>
/// Represents the a memo record with an associated type.
/// </summary>
/// <param name="Type">Gets the type of the memo record.</param>
/// <param name="Data">Gets the data of the memo record.</param>
public readonly record struct MemoRecord(MemoRecordType Type, ReadOnlyMemory<byte> Data)
{
    /// <summary>
    /// Gets the length of the memo record in bytes.
    /// </summary>
    public int Length => Data.Length;

    /// <summary>
    /// Gets the data of the memo record as a span.
    /// </summary>
    public ReadOnlySpan<byte> Span => Data.Span;
}
