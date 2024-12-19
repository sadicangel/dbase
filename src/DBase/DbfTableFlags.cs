namespace DBase;

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
    /// Indicates the presence of memo fields in the table.
    /// Memo fields are used to store large text or binary data.
    /// </summary>
    HasMemoField = 0x02,

    /// <summary>
    /// Indicates that the table is part of a database container (DBC).
    /// A DBC contains metadata about the database, such as table definitions and relationships.
    /// </summary>
    IsDbc = 0x04
}
