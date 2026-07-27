namespace DBase;

/// <summary>
/// Identifies the DBF serialization operation that was running when an error occurred.
/// </summary>
public enum DbfSerializationOperation
{
    /// <summary>
    /// A record field was being read from DBF bytes.
    /// </summary>
    Read,

    /// <summary>
    /// A record field was being written to DBF bytes.
    /// </summary>
    Write,
}
