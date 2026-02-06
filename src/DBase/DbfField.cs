using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DBase;

/// <summary>
/// Represents a value in a DBF record field.
/// </summary>
/// <seealso cref="DbfFieldDescriptor" />
/// <remarks>
/// A <see cref="DbfField" /> is a lightweight wrapper around the raw value read from or written to a
/// <see cref="DbfRecord" />. The value is stored as <see cref="object" /> and may be <see langword="null" />
/// when the underlying field is empty or null.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly struct DbfField : IEquatable<DbfField>
{
    private readonly object? _value;

    /// <summary>
    /// Gets the raw value of the field.
    /// </summary>
    public object? Value => _value;

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsNull => _value is null;

    private DbfField(object? value) => _value = value;

    /// <summary>
    /// Determines whether this instance stores a value of the exact type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>
    /// <see langword="true" /> if this instance is of type <typeparamref name="T"/>; otherwise, <see langword="false" />.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsType<T>() => _value is not null && typeof(T) == _value.GetType();

    /// <summary>
    /// Gets the value of the field as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <returns>
    /// The stored value when it is of type <typeparamref name="T"/>; otherwise <see langword="default" />.
    /// </returns>
    public T? GetValue<T>() => _value is T value ? value : default;

    /// <summary>
    /// Gets the string representation of the field.
    /// </summary>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Gets the string representation of the field using the specified format provider.
    /// </summary>
    /// <param name="provider">An optional format provider.</param>
    public string ToString(IFormatProvider? provider) => string.Format(provider, "{0}", _value);

    private string GetDebuggerDisplay() => ToString();

    /// <inheritdoc/>
    public bool Equals(DbfField other) => _value?.Equals(other._value) ?? other._value is null;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DbfField field && Equals(field);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_value);

    /// <summary>Determines whether two specified <see cref="DbfField"/> values are equal.</summary>
    public static bool operator ==(DbfField left, DbfField right) => left.Equals(right);

    /// <summary>Determines whether two specified <see cref="DbfField"/> values are not equal.</summary>
    public static bool operator !=(DbfField left, DbfField right) => !(left == right);

    /// <summary>Converts a <see cref="bool"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(bool boolean) => new(boolean);

    /// <summary>Converts a nullable <see cref="bool"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(bool? boolean) => new(boolean);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="bool"/>.</summary>
    public static explicit operator bool?(DbfField field) => (bool?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="bool"/>.</summary>
    public static explicit operator bool(DbfField field) =>
        field._value is bool value ? value : throw new InvalidCastException();

    /// <summary>Converts an <see cref="int"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(int number) => new(number);

    /// <summary>Converts a nullable <see cref="int"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(int? number) => new(number);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="int"/>.</summary>
    public static explicit operator int?(DbfField field) => (int?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to an <see cref="int"/>.</summary>
    public static explicit operator int(DbfField field) =>
        field._value is int value ? value : throw new InvalidCastException();

    /// <summary>Converts a <see cref="long"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(long number) => new(number);

    /// <summary>Converts a nullable <see cref="long"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(long? number) => new(number);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="long"/>.</summary>
    public static explicit operator long?(DbfField field) => (long?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="long"/>.</summary>
    public static explicit operator long(DbfField field) =>
        field._value is long value ? value : throw new InvalidCastException();

    /// <summary>Converts a <see cref="double"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(double number) => new(number);

    /// <summary>Converts a nullable <see cref="double"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(double? number) => new(number);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="double"/>.</summary>
    public static explicit operator double?(DbfField field) => (double?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="double"/>.</summary>
    public static explicit operator double(DbfField field) =>
        field._value is double value ? value : throw new InvalidCastException();

    /// <summary>Converts a <see cref="decimal"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(decimal number) => new(number);

    /// <summary>Converts a nullable <see cref="decimal"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(decimal? number) => new(number);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="decimal"/>.</summary>
    public static explicit operator decimal?(DbfField field) => (decimal?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="decimal"/>.</summary>
    public static explicit operator decimal(DbfField field) =>
        field._value is decimal value ? value : throw new InvalidCastException();

    /// <summary>Converts a <see cref="DateTime"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(DateTime dateTime) => new(dateTime);

    /// <summary>Converts a nullable <see cref="DateTime"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(DateTime? dateTime) => new(dateTime);

    /// <summary>Converts a <see cref="DbfField"/> to a nullable <see cref="DateTime"/>.</summary>
    public static explicit operator DateTime?(DbfField field) => (DateTime?)field._value;

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="DateTime"/>.</summary>
    public static explicit operator DateTime(DbfField field) =>
        field._value is DateTime value ? value : throw new InvalidCastException();

    /// <summary>Converts a <see cref="string"/> to a <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(string? text) => new(text);

    /// <summary>Converts a <see cref="DbfField"/> to a <see cref="string"/>.</summary>
    public static explicit operator string?(DbfField field) => (string?)field._value;
}
