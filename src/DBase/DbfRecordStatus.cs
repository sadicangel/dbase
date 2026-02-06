namespace DBase;

/// <summary>
/// Defines the on-disk status marker stored at the beginning of a DBF record.
/// </summary>
public enum DbfRecordStatus : byte
{
    /// <summary>
    /// Active record marker (<c>0x20</c>, ASCII space).
    /// </summary>
    Valid = 0x20,

    /// <summary>
    /// Deleted record marker (<c>0x2A</c>, ASCII <c>*</c>).
    /// </summary>
    Deleted = 0x2A,
}
