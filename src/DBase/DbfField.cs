using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DBase.Interop;

namespace DBase;

/// <summary>
/// Represents a value in a DBF record field.
/// </summary>
/// <seealso cref="DbfFieldDescriptor" />
/// <remarks>
/// A <see cref="DbfField" /> is a lightweight wrapper around the raw value read from or written to a
/// <see cref="DbfRecord" />. Values are stored inline except for strings, and may be
/// <see langword="null" /> when the underlying field is empty or null.
/// </remarks>
[Union]
[StructLayout(LayoutKind.Explicit)]
public readonly struct DbfField : IUnion, IEquatable<DbfField>
{
    // BinaryDecimal has 13 meaningful bytes and 3 padding bytes. The tag shares its last padding byte.
    // Constructors must assign the payload before the tag, since a whole payload copy can include padding.
    [FieldOffset(0)] private readonly string? _string;
    [FieldOffset(8)] private readonly bool _bool;
    [FieldOffset(8)] private readonly int _int;
    [FieldOffset(8)] private readonly long _long;
    [FieldOffset(8)] private readonly double _double;
    [FieldOffset(8)] private readonly DateTime _dateTime;
    [FieldOffset(8)] private readonly BinaryDecimal _decimal;
    [FieldOffset(23)] private readonly DbfFieldTag _tag;

    private enum DbfFieldTag : byte
    {
        Null = 0,
        String = 1,
        Boolean = 2,
        Int32 = 3,
        Int64 = 4,
        Double = 5,
        Decimal = 6,
        DateTime = 7
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified Boolean value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(bool value)
    {
        _bool = value;
        _tag = DbfFieldTag.Boolean;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable Boolean value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(bool? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified 32-bit signed integer.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(int value)
    {
        _int = value;
        _tag = DbfFieldTag.Int32;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable 32-bit integer.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(int? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified 64-bit signed integer.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(long value)
    {
        _long = value;
        _tag = DbfFieldTag.Int64;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable 64-bit integer.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(long? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified double-precision floating-point value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(double value)
    {
        _double = value;
        _tag = DbfFieldTag.Double;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable double-precision value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(double? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified decimal value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(decimal value)
    {
        _decimal = BinaryDecimal.FromDecimal(value);
        _tag = DbfFieldTag.Decimal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable decimal value.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(decimal? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with the specified date and time.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(DateTime value)
    {
        _dateTime = value;
        _tag = DbfFieldTag.DateTime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable date and time.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(DateTime? value) =>
        this = value.HasValue ? new DbfField(value.GetValueOrDefault()) : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField"/> structure with a nullable string.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public DbfField(string? value)
    {
        if (value is null) return;
        _string = value;
        _tag = DbfFieldTag.String;
    }

    /// <summary>
    /// Gets the raw value of the field.
    /// </summary>
    /// <remarks>Value types are boxed when accessed through this property.</remarks>
    public object? Value => _tag switch
    {
        DbfFieldTag.String => _string,
        DbfFieldTag.Boolean => _bool,
        DbfFieldTag.Int32 => _int,
        DbfFieldTag.Int64 => _long,
        DbfFieldTag.Double => _double,
        DbfFieldTag.Decimal => _decimal.ToDecimal(),
        DbfFieldTag.DateTime => _dateTime,
        _ => null
    };

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsNull => _tag == DbfFieldTag.Null;

    /// <summary>
    /// Gets a value indicating whether this instance contains a non-null value.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue => !IsNull;

    /// <summary>
    /// Determines whether this instance stores a value of the exact type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>
    /// <see langword="true" /> if this instance is of type <typeparamref name="T"/>; otherwise, <see langword="false" />.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsType<T>() => _tag switch
    {
        DbfFieldTag.String => typeof(T) == typeof(string),
        DbfFieldTag.Boolean => typeof(T) == typeof(bool),
        DbfFieldTag.Int32 => typeof(T) == typeof(int),
        DbfFieldTag.Int64 => typeof(T) == typeof(long),
        DbfFieldTag.Double => typeof(T) == typeof(double),
        DbfFieldTag.Decimal => typeof(T) == typeof(decimal),
        DbfFieldTag.DateTime => typeof(T) == typeof(DateTime),
        _ => false
    };

    /// <summary>
    /// Attempts to retrieve the <see cref="bool"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="bool"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out bool value)
    {
        var success = _tag == DbfFieldTag.Boolean;
        value = success && _bool;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="int"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="int"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out int value)
    {
        var success = _tag == DbfFieldTag.Int32;
        value = success ? _int : 0;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="long"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="long"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out long value)
    {
        var success = _tag == DbfFieldTag.Int64;
        value = success ? _long : 0;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="double"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="double"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out double value)
    {
        var success = _tag == DbfFieldTag.Double;
        value = success ? _double : 0;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="decimal"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="decimal"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out decimal value)
    {
        var success = _tag == DbfFieldTag.Decimal;
        value = success ? _decimal.ToDecimal() : 0;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="DateTime"/> value stored in this instance.
    /// </summary>
    /// <param name="value">The stored value on success; otherwise, the default value.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a <see cref="DateTime"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue(out DateTime value)
    {
        var success = _tag == DbfFieldTag.DateTime;
        value = success ? _dateTime : default;
        return success;
    }

    /// <summary>
    /// Attempts to retrieve the string value stored in this instance.
    /// </summary>
    /// <param name="value">The stored string on success; otherwise, null.</param>
    /// <returns>
    /// <see langword="true"/> if this instance contains a string; otherwise, <see langword="false"/>.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue([NotNullWhen(true)] out string? value)
    {
        value = _tag == DbfFieldTag.String ? _string : null;
        return value is not null;
    }

    /// <summary>
    /// Retrieves the stored value as the specified type.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <returns>
    /// The stored value if it is assignable to <typeparamref name="T"/>; otherwise, the default value of <typeparamref name="T"/>.
    /// </returns>
    /// <remarks>
    /// Value-type and nullable value-type results do not require boxing. Retrieving a value type as
    /// <see cref="object"/> or an implemented interface boxes the value.
    /// </remarks>
    public T? GetValue<T>() => _tag switch
    {
        DbfFieldTag.String => _string is T value ? value : default,
        DbfFieldTag.Boolean => GetValue<T, bool>(_bool),
        DbfFieldTag.Int32 => GetValue<T, int>(_int),
        DbfFieldTag.Int64 => GetValue<T, long>(_long),
        DbfFieldTag.Double => GetValue<T, double>(_double),
        DbfFieldTag.Decimal => GetValue<T, decimal>(_decimal.ToDecimal()),
        DbfFieldTag.DateTime => GetValue<T, DateTime>(_dateTime),
        _ => default
    };

    /// <summary>
    /// Gets the string representation of the field.
    /// </summary>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Gets the string representation of the field using the specified format provider.
    /// </summary>
    /// <param name="provider">An optional format provider.</param>
    public string ToString(IFormatProvider? provider)
    {
        // ICustomFormatter accepts object, so preserve its existing behavior at that boxing boundary.
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter formatter &&
            formatter.Format(null, Value, provider) is { } formatted)
            return formatted;

        return this switch
        {
            string value => value,
            bool value => value.ToString(provider),
            int value => value.ToString(provider),
            long value => value.ToString(provider),
            double value => value.ToString(provider),
            decimal value => value.ToString(provider),
            DateTime value => value.ToString(provider),
            null => string.Empty
        };
    }

    /// <inheritdoc/>
    public bool Equals(DbfField other) => _tag == other._tag && _tag switch
    {
        DbfFieldTag.String => _string == other._string,
        DbfFieldTag.Boolean => _bool == other._bool,
        DbfFieldTag.Int32 => _int == other._int,
        DbfFieldTag.Int64 => _long == other._long,
        DbfFieldTag.Double => _double.Equals(other._double),
        DbfFieldTag.Decimal => _decimal.ToDecimal() == other._decimal.ToDecimal(),
        DbfFieldTag.DateTime => _dateTime == other._dateTime,
        _ => true
    };

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DbfField field && Equals(field);

    /// <inheritdoc/>
    public override int GetHashCode() => _tag switch
    {
        DbfFieldTag.String => HashCode.Combine(_string),
        DbfFieldTag.Boolean => HashCode.Combine(_bool),
        DbfFieldTag.Int32 => HashCode.Combine(_int),
        DbfFieldTag.Int64 => HashCode.Combine(_long),
        DbfFieldTag.Double => HashCode.Combine(_double),
        DbfFieldTag.Decimal => HashCode.Combine(_decimal.ToDecimal()),
        DbfFieldTag.DateTime => HashCode.Combine(_dateTime),
        _ => HashCode.Combine<object?>(null)
    };

    /// <summary>
    /// Determines whether two specified <see cref="DbfField"/> values are equal.
    /// </summary>
    public static bool operator ==(DbfField left, DbfField right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified <see cref="DbfField"/> values are not equal.
    /// </summary>
    public static bool operator !=(DbfField left, DbfField right) => !(left == right);

    /// <summary>
    /// Converts a <see cref="bool"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(bool boolean) => new(boolean);

    /// <summary>
    /// Converts a nullable <see cref="bool"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(bool? boolean) => new(boolean);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="bool"/>.
    /// </summary>
    public static explicit operator bool?(DbfField field) => field switch
    {
        bool value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(bool?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="bool"/>.
    /// </summary>
    public static explicit operator bool(DbfField field) => field switch
    {
        bool value => value,
        _ => throw field.CreateInvalidCastException(typeof(bool))
    };

    /// <summary>
    /// Converts an <see cref="int"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(int number) => new(number);

    /// <summary>
    /// Converts a nullable <see cref="int"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(int? number) => new(number);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="int"/>.
    /// </summary>
    public static explicit operator int?(DbfField field) => field switch
    {
        int value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(int?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to an <see cref="int"/>.
    /// </summary>
    public static explicit operator int(DbfField field) => field switch
    {
        int value => value,
        _ => throw field.CreateInvalidCastException(typeof(int))
    };

    /// <summary>
    /// Converts a <see cref="long"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(long number) => new(number);

    /// <summary>
    /// Converts a nullable <see cref="long"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(long? number) => new(number);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="long"/>.
    /// </summary>
    public static explicit operator long?(DbfField field) => field switch
    {
        long value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(long?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="long"/>.
    /// </summary>
    public static explicit operator long(DbfField field) => field switch
    {
        long value => value,
        _ => throw field.CreateInvalidCastException(typeof(long))
    };

    /// <summary>
    /// Converts a <see cref="double"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(double number) => new(number);

    /// <summary>
    /// Converts a nullable <see cref="double"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(double? number) => new(number);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="double"/>.
    /// </summary>
    public static explicit operator double?(DbfField field) => field switch
    {
        double value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(double?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="double"/>.
    /// </summary>
    public static explicit operator double(DbfField field) => field switch
    {
        double value => value,
        _ => throw field.CreateInvalidCastException(typeof(double))
    };

    /// <summary>
    /// Converts a <see cref="decimal"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(decimal number) => new(number);

    /// <summary>
    /// Converts a nullable <see cref="decimal"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(decimal? number) => new(number);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="decimal"/>.
    /// </summary>
    public static explicit operator decimal?(DbfField field) => field switch
    {
        decimal value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(decimal?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="decimal"/>.
    /// </summary>
    public static explicit operator decimal(DbfField field) => field switch
    {
        decimal value => value,
        _ => throw field.CreateInvalidCastException(typeof(decimal))
    };

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(DateTime dateTime) => new(dateTime);

    /// <summary>
    /// Converts a nullable <see cref="DateTime"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(DateTime? dateTime) => new(dateTime);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a nullable <see cref="DateTime"/>.
    /// </summary>
    public static explicit operator DateTime?(DbfField field) => field switch
    {
        DateTime value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(DateTime?))
    };

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="DateTime"/>.
    /// </summary>
    public static explicit operator DateTime(DbfField field) => field switch
    {
        DateTime value => value,
        _ => throw field.CreateInvalidCastException(typeof(DateTime))
    };

    /// <summary>
    /// Converts a <see cref="string"/> to a <see cref="DbfField"/>.
    /// </summary>
    public static implicit operator DbfField(string? text) => new(text);

    /// <summary>
    /// Converts a <see cref="DbfField"/> to a <see cref="string"/>.
    /// </summary>
    public static explicit operator string?(DbfField field) => field switch
    {
        string value => value,
        null => null,
        _ => throw field.CreateInvalidCastException(typeof(string))
    };

    private static T? GetValue<T, TValue>(TValue value) where TValue : struct
    {
        // Each reinterpretation is guarded by exact type equality, including the nullable layout.
        if (typeof(T) == typeof(TValue))
            return Unsafe.As<TValue, T>(ref value);
        if (typeof(T) == typeof(TValue?))
        {
            TValue? nullable = value;
            return Unsafe.As<TValue?, T>(ref nullable);
        }

        // Requests for object or an implemented interface require boxing by their return type.
        return !typeof(T).IsValueType && typeof(T).IsAssignableFrom(typeof(TValue))
            ? (T)(object)value
            : default;
    }

    private InvalidCastException CreateInvalidCastException(Type targetType)
    {
        var targetName = Nullable.GetUnderlyingType(targetType) is { } underlyingType
            ? $"{underlyingType.Name}?"
            : targetType.Name;

        return new InvalidCastException(IsNull
            ? $"Cannot cast a null DbfField to '{targetName}'."
            : $"Cannot cast a DbfField containing '{_tag}' to '{targetName}'.");
    }
}
