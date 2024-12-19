namespace DBase;

/// <summary>
/// Describes the byte that represents the status of a record.
/// </summary>
public enum DbfRecordStatus : byte
{
    Active = 0x20,
    Deleted = 0x2A,
}
