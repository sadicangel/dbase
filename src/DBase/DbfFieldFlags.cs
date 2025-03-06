namespace DBase;

/// <summary>
/// Represents the flags of a <see cref="DbfField" />.
/// </summary>
[Flags]
public enum DbfFieldFlags : byte
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0x0,
    /// <summary>
    /// System field.
    /// </summary>
    System = 0x1,
    /// <summary>
    /// Nullable field.
    /// </summary>
    Nullable = 0x2,
    /// <summary>
    /// Binary field.
    /// </summary>
    Binary = 0x4,
    /// <summary>
    /// Auto increment field.
    /// </summary>
    AutoIncrement = 0xC,
}
