namespace DBase;

/// <summary>
/// Defines per-field attribute bits stored in a DBF field descriptor.
/// </summary>
/// <remarks>
/// These values map to the flag byte in <see cref="DbfFieldDescriptor.Flags"/>. Flags are primarily used by
/// Visual FoxPro variants and may be combined bitwise.
/// </remarks>
[Flags]
public enum DbfFieldFlags : byte
{
    /// <summary>
    /// No special attributes are set.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Indicates a system-managed field.
    /// </summary>
    System = 0x1,

    /// <summary>
    /// Indicates the field can contain null values.
    /// </summary>
    Nullable = 0x2,

    /// <summary>
    /// Indicates binary storage semantics for the field.
    /// </summary>
    Binary = 0x4,

    /// <summary>
    /// Indicates an auto-increment field.
    /// </summary>
    /// <remarks>
    /// This is a composite value (<c>0x0C</c>) used by FoxPro-style descriptors.
    /// </remarks>
    AutoIncrement = 0xC,
}
