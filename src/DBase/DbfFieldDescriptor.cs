using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DBase;

/// <summary>
/// Describes a <see cref="DbfField" />.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly record struct DbfFieldDescriptor : IEquatable<DbfFieldDescriptor>
{
    internal const int Size = 32;

    /// <summary>
    /// Gets the field name in ASCII.
    /// </summary>
    [field: FieldOffset(0)]
    public readonly DbfFieldName Name { get; init; }

    [FieldOffset(10)]
    private readonly byte _zero; // '\0'

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    [field: FieldOffset(11)]
    public readonly DbfFieldType Type { get; init; }

    /// <summary>
    /// Gets the offset of the field in the record.
    /// </summary>
    [field: FieldOffset(12)]
    public readonly int Offset { get; init; }

    /// <summary>
    /// Gets or sets the length of the field in binary (maximum 254).
    /// </summary>
    [field: FieldOffset(16)]
    public readonly byte Length { get; init; }

    /// <summary>
    /// Gets the field decimal count in binary.
    /// </summary>
    [field: FieldOffset(17)]
    public readonly byte Decimal { get; init; }

    /// <summary>
    /// Gets the field flags describing special attributes of the field.
    /// </summary>
    [field: FieldOffset(18)]
    public readonly DbfFieldFlags Flags { get; init; }

    /// <summary>
    /// Gets the auto-increment next value.
    /// </summary>
    [field: FieldOffset(19)]
    internal readonly int AutoIncrementNextValue { get; init; }

    /// <summary>
    /// Gets the auto-increment step value.
    /// </summary>
    [field: FieldOffset(23)]
    internal readonly byte AutoIncrementStepValue { get; init; }

    [FieldOffset(24)]
    private readonly long _reserved8;

    private unsafe string GetDebuggerDisplay() => $"{Name},{(char)Type},{Length},{Decimal}";

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Character" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Text(DbfFieldName name, byte length)
    {
        if (!name.IsValid)
        {
            throw new ArgumentException("Invalid field name", nameof(name));
        }

        return new()
        {
            Name = name,
            Type = DbfFieldType.Character,
            Length = length,
            Decimal = 0,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (total number of digits).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Number(DbfFieldName name, byte length, byte @decimal = 0)
    {
        if (!name.IsValid)
        {
            throw new ArgumentException("Invalid field name", nameof(name));
        }
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 20);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(@decimal, length);

        return new()
        {
            Name = name,
            Type = DbfFieldType.Numeric,
            Length = length,
            Decimal = @decimal,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="byte" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Byte(DbfFieldName name) => Number(name, length: 3);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="short" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Int16(DbfFieldName name) => Number(name, 6);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="int" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Int32(DbfFieldName name) => Number(name, 10);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="long" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Int64(DbfFieldName name) => Number(name, 20);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="float" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Single(DbfFieldName name) => Number(name, 14, 7);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />
    /// that is large enough to store a <see cref="double" /> value.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Double(DbfFieldName name) => Number(name, 30, 15);

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Date" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Date(DbfFieldName name)
    {
        if (!name.IsValid)
        {
            throw new ArgumentException("Invalid field name", nameof(name));
        }

        return new()
        {
            Name = name,
            Type = DbfFieldType.Date,
            Length = 8,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Logical" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Boolean(DbfFieldName name)
    {
        if (!name.IsValid)
        {
            throw new ArgumentException("Invalid field name", nameof(name));
        }

        return new()
        {
            Name = name,
            Type = DbfFieldType.Logical,
            Length = 1,
            Decimal = 0
        };
    }
}
