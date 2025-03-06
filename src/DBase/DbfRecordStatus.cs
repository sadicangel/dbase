namespace DBase;

/// <summary>
/// Describes the byte that represents the status of a record.
/// </summary>
public enum DbfRecordStatus : byte
{
    /// <summary>
    /// Record is valid.
    /// </summary>
    Valid = 0x20,
    /// <summary>
    /// Record is deleted.
    /// </summary>
    Deleted = 0x2A,
}
