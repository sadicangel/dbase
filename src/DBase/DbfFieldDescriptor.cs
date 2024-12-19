using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace DBase;

/// <summary>
/// Describes a <see cref="DbfField" />.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly struct DbfFieldDescriptor : IEquatable<DbfFieldDescriptor>
{
    internal const int Size = 32;

    [FieldOffset(0)]
    private readonly DbfFieldName _name;
    [FieldOffset(10)]
    private readonly byte _zero; // '\0'
    [FieldOffset(11)]
    private readonly DbfFieldType _type;
    [FieldOffset(12)]
    private readonly int _address; // in memory address.
    [FieldOffset(16)]
    private readonly byte _length;
    [FieldOffset(17)]
    private readonly byte _decimal;
    [FieldOffset(20)]
    private readonly ushort _workAreaId;
    [FieldOffset(23)]
    private readonly int _setFields;
    [FieldOffset(31)]
    private readonly byte _inMdxFile;

    /// <summary>
    /// Gets the field name in ASCII.
    /// </summary>
    public readonly DbfFieldName Name { get => _name; init => _name = value; }

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    public readonly DbfFieldType Type { get => _type; init => _type = value; }

    /// <summary>
    /// Gets or sets the length of the field in binary (maximum 254).
    /// </summary>
    public readonly byte Length { get => _length; init => _length = value; }

    /// <summary>
    /// Gets the field decimal count in binary.
    /// </summary>
    public readonly byte Decimal { get => _decimal; init => _decimal = value; }

    /// <summary>
    /// Gets the field name in UTF16.
    /// </summary>
    public readonly string GetNameUtf16() => Encoding.ASCII.GetString(_name);

    internal readonly bool NameUtf16Equals(ReadOnlySpan<char> name)
    {
        if (name is { Length: 0 or > 10 })
            return false;

        Span<byte> buffer = stackalloc byte[Encoding.ASCII.GetMaxByteCount(name.Length)];
        var bytes = Encoding.ASCII.GetBytes(name, buffer);

        return ((ReadOnlySpan<byte>)_name).SequenceEqual(buffer[..bytes]);
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfFieldDescriptor other)
    {
        //return MemoryMarshal.AsBytes([this]).SequenceEqual(MemoryMarshal.AsBytes([other]));
        return _name == other._name
            && _zero == other._zero
            && _type == other._type
            && _address == other._address
            && _length == other._length
            && _decimal == other._decimal
            && _workAreaId == other._workAreaId
            && _setFields == other._setFields
            && _inMdxFile == other._inMdxFile;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    /// <returns>
    ///   <see langword="true" /> if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => obj is DbfFieldDescriptor descriptor && Equals(descriptor);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_name);
        hash.Add(_zero);
        hash.Add(_type);
        hash.Add(_address);
        hash.Add(_length);
        hash.Add(_decimal);
        hash.Add(_workAreaId);
        hash.Add(_setFields);
        hash.Add(_inMdxFile);
        return hash.ToHashCode();
    }

    private unsafe string GetDebuggerDisplay() => $"{_name},{(char)_type},{_length},{_decimal}";

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

    /// <summary>
    /// Implements the operator op_Equality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(DbfFieldDescriptor left, DbfFieldDescriptor right) => left.Equals(right);

    /// <summary>
    /// Implements the operator op_Inequality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(DbfFieldDescriptor left, DbfFieldDescriptor right) => !(left == right);
}
