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
    [FieldOffset(12)]
    internal readonly int Offset;

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

    [FieldOffset(19)]
    internal readonly int AutoIncrementNextValue;

    [FieldOffset(23)]
    internal readonly byte AutoIncrementStepValue;

    [FieldOffset(24)]
    private readonly long _reserved8;

    private unsafe string GetDebuggerDisplay() => $"{Name},{(char)Type},{Length},{Decimal}";

    private static void ThrowIfInvalidName(DbfFieldName name)
    {
        if (!name.IsValid)
        {
            throw new ArgumentException("Invalid field name", nameof(name));
        }
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.AutoIncrement" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor AutoIncrement(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.AutoIncrement,
            Length = 8,
            Flags = DbfFieldFlags.AutoIncrement,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Binary" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).
    /// If <c>8</c>, the field stores <see cref="double" /> values;
    /// otherwise, it stores the index to a memo file as a right-justified ASCII string.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Binary(DbfFieldName name, byte length = 10)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Binary,
            Length = length,
            Decimal = 0,
            Flags = length is 8 ? default : DbfFieldFlags.Binary
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Blob" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Blob(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Blob,
            Length = 10,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Character" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The <paramref name="length"/> of the field (number of ASCII characters).</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Character(DbfFieldName name, byte length)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Character,
            Length = length,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Currency" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Currency(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Currency,
            Length = 8,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Date" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Date(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Date,
            Length = 8,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.DateTime" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor DateTime(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.DateTime,
            Length = 8,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Double" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Double(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Double,
            Length = 8,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Float" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (total number of digits).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Float(DbfFieldName name, byte length, byte @decimal)
    {
        ThrowIfInvalidName(name);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 20);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(@decimal, length);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Float,
            Length = length,
            Decimal = @decimal,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Int32" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Int32(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Int32,
            Length = 4,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Logical" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Logical(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Logical,
            Length = 1,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Memo" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).
    /// If <c>4</c>, the field stores memo indices as <see cref="int" /> values;
    /// otherwise, it stores the indices as a right-justified string.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Memo(DbfFieldName name, byte length = 10)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Memo,
            Length = length,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.NullFlags" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).
    /// Must be enough to represent all fields in the record.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor NullFlags(DbfFieldName name, byte length)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.NullFlags,
            Length = length,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (total number of digits).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Numeric(DbfFieldName name, byte length = 10, byte @decimal = 0)
    {
        ThrowIfInvalidName(name);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 20);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(@decimal, length);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Float,
            Length = length,
            Decimal = @decimal,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Ole" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).
    /// If <c>4</c>, the field stores memo indices as <see cref="int" /> values;
    /// otherwise, it stores the indices as a right-justified string.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Ole(DbfFieldName name, byte length = 10)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Ole,
            Length = length,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Picture" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).
    /// If <c>4</c>, the field stores memo indices as <see cref="int" /> values;
    /// otherwise, it stores the indices as a right-justified string.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Picture(DbfFieldName name, byte length = 10)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Picture,
            Length = length,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Timestamp" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Timestamp(DbfFieldName name)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Timestamp,
            Length = 8,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Variant" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of bytes).</param>
    /// <returns>A new instance of <see cref="DbfFieldDescriptor" /></returns>
    public static DbfFieldDescriptor Variant(DbfFieldName name, byte length)
    {
        ThrowIfInvalidName(name);

        return new DbfFieldDescriptor()
        {
            Name = name,
            Type = DbfFieldType.Variant,
            Length = length,
        };
    }
}
