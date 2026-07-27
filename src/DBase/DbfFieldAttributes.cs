using System.Reflection;

namespace DBase;

/// <summary>
/// Excludes a public model property from typed DBF descriptor generation and serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfIgnoreAttribute : Attribute;

/// <summary>
/// Base class for attributes that explicitly configure the DBF field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public abstract class DbfFieldAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the DBF field name. When unset, the CLR property name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional descriptor flags applied to the generated field descriptor.
    /// </summary>
    public DbfFieldFlags Flags { get; set; }

    internal DbfFieldDescriptor CreateDescriptor(PropertyInfo property)
    {
        var name = new DbfFieldName(Name ?? property.Name);
        var descriptor = CreateDescriptorCore(name);
        return descriptor with { Flags = descriptor.Flags | Flags };
    }

    internal abstract DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name);
}

/// <summary>
/// Configures a character field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfCharacterAttribute(byte length) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Character(name, Length);
}

/// <summary>
/// Configures a numeric field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
/// <param name="decimal">Number of decimal digits.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfNumericAttribute(byte length = 10, byte @decimal = 0) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    /// <summary>
    /// Gets the number of decimal digits.
    /// </summary>
    public byte Decimal { get; } = @decimal;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Numeric(name, Length, Decimal);
}

/// <summary>
/// Configures a floating-point numeric field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
/// <param name="decimal">Number of decimal digits.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfFloatAttribute(byte length, byte @decimal) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    /// <summary>
    /// Gets the number of decimal digits.
    /// </summary>
    public byte Decimal { get; } = @decimal;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Float(name, Length, Decimal);
}

/// <summary>
/// Configures a binary field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfBinaryAttribute(byte length = 10) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Binary(name, Length);
}

/// <summary>
/// Configures a memo field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfMemoAttribute(byte length = 10) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Memo(name, Length);
}

/// <summary>
/// Configures an OLE field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfOleAttribute(byte length = 10) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Ole(name, Length);
}

/// <summary>
/// Configures a picture field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfPictureAttribute(byte length = 10) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Picture(name, Length);
}

/// <summary>
/// Configures a null-flags field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfNullFlagsAttribute(byte length) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.NullFlags(name, Length);
}

/// <summary>
/// Configures a variant field descriptor for a model property.
/// </summary>
/// <param name="length">Field length in bytes.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfVariantAttribute(byte length) : DbfFieldAttribute
{
    /// <summary>
    /// Gets the field length in bytes.
    /// </summary>
    public byte Length { get; } = length;

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Variant(name, Length);
}

/// <summary>
/// Configures a dBASE Level 7 auto-increment field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfAutoIncrementAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfAutoIncrementAttribute"/> class.
    /// </summary>
    public DbfAutoIncrementAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.AutoIncrement(name);
}

/// <summary>
/// Configures a blob field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfBlobAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfBlobAttribute"/> class.
    /// </summary>
    public DbfBlobAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Blob(name);
}

/// <summary>
/// Configures a currency field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfCurrencyAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfCurrencyAttribute"/> class.
    /// </summary>
    public DbfCurrencyAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Currency(name);
}

/// <summary>
/// Configures a date field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfDateAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfDateAttribute"/> class.
    /// </summary>
    public DbfDateAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Date(name);
}

/// <summary>
/// Configures a datetime field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfDateTimeAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfDateTimeAttribute"/> class.
    /// </summary>
    public DbfDateTimeAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.DateTime(name);
}

/// <summary>
/// Configures a double-precision binary field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfDoubleAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfDoubleAttribute"/> class.
    /// </summary>
    public DbfDoubleAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Double(name);
}

/// <summary>
/// Configures a 32-bit integer field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfInt32Attribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfInt32Attribute"/> class.
    /// </summary>
    public DbfInt32Attribute() { }

    /// <summary>
    /// Gets or sets a value indicating whether to mark the integer field as Visual FoxPro auto-increment.
    /// </summary>
    public bool AutoIncrement { get; set; }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name)
    {
        var descriptor = DbfFieldDescriptor.Int32(name);
        return AutoIncrement ? descriptor with { Flags = DbfFieldFlags.AutoIncrement } : descriptor;
    }
}

/// <summary>
/// Configures a logical field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfLogicalAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfLogicalAttribute"/> class.
    /// </summary>
    public DbfLogicalAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Logical(name);
}

/// <summary>
/// Configures a timestamp field descriptor for a model property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbfTimestampAttribute : DbfFieldAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DbfTimestampAttribute"/> class.
    /// </summary>
    public DbfTimestampAttribute() { }

    internal override DbfFieldDescriptor CreateDescriptorCore(DbfFieldName name) =>
        DbfFieldDescriptor.Timestamp(name);
}
