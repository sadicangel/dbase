namespace DBase;

/// <summary>
/// Defines table-level attribute bits stored in the DBF header.
/// </summary>
/// <remarks>
/// Values map to the table-flags byte and may be combined bitwise.
/// </remarks>
[Flags]
public enum DbfTableFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Indicates the presence of a structural .cdx (compound index) file.
    /// </summary>
    HasStructuralCdx = 0x01,

    /// <summary>
    /// Indicates the table contains at least one memo-capable field.
    /// </summary>
    HasMemoField = 0x02,

    /// <summary>
    /// Indicates the table is associated with a database container (DBC).
    /// </summary>
    IsDbc = 0x04
}
