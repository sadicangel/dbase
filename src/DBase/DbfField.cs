using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DBase;

/// <summary>
/// Represents a field of a <see cref="DbfRecord" />.
/// </summary>
/// <seealso cref="IEquatable{T}" />
/// <remarks>
/// The field is defined by a <see cref="DbfFieldDescriptor" />.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public struct DbfField : IEquatable<DbfField>
{
    // The stored value.
    // If strings were not supported we could have used 8 bytes to store all types
    // and avoid boxing/unboxing, but it is what it is.
    internal object? _value;

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_value))]
    public readonly bool IsNull => _value is null;

    /// <summary>
    /// Determines whether this instance is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>
    ///   <see langword="true" /> if this instance is of type <typeparamref name="T"/>; otherwise, <see langword="false" />.
    /// </returns>
    public readonly bool IsType<T>() => _value is not null && typeof(T) == _value.GetType();

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override readonly string ToString() => FormattableString.Invariant($"{_value}");

    private readonly string GetDebuggerDisplay() => ToString();

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public readonly bool Equals(DbfField other) => _value is null ? other._value is null : _value.Equals(other._value);

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override readonly bool Equals(object? obj) => obj is DbfField field && Equals(field);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override readonly int GetHashCode() => HashCode.Combine(_value);

    /// <summary>
    /// Implements the operator op_Equality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(DbfField left, DbfField right) => left.Equals(right);

    /// <summary>
    /// Implements the operator op_Inequality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(DbfField left, DbfField right) => !(left == right);

    /// <summary>
    /// Performs an implicit conversion from <see cref="bool"/> to <see cref="DbfField"/>.
    /// </summary>
    /// <param name="boolean">if set to <see langword="true" /> [boolean].</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator DbfField(bool boolean) => new() { _value = boolean };
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="bool" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator bool?(DbfField field) => (bool?)field._value;
    /// <summary>
    /// Performs an explicit conversion from <see cref="DbfField"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator bool(DbfField field) => field._value is not null ? (bool)field._value : throw new InvalidCastException();

    /// <summary>
    /// Performs an implicit conversion from <see cref="long"/> to <see cref="DbfField"/>.
    /// </summary>
    /// <param name="number">The number value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator DbfField(long number) => new() { _value = number };
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="long" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator long?(DbfField field) => (long?)field._value;
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="long" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator long(DbfField field) => field._value is not null ? (long)field._value : throw new InvalidCastException();

    /// <summary>
    /// Performs an implicit conversion from <see cref="double"/> to <see cref="DbfField"/>.
    /// </summary>
    /// <param name="number">The number value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator DbfField(double number) => new() { _value = number };
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator double?(DbfField field) => (double?)field._value;
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator double(DbfField field) => field._value is not null ? (double)field._value : throw new InvalidCastException();

    /// <summary>
    /// Performs an implicit conversion from <see cref="DateTime" /> to <see cref="DbfField" />.
    /// </summary>
    /// <param name="dateTime">The date time value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator DbfField(DateTime dateTime) => new() { _value = dateTime };
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator DateTime?(DbfField field) => (DateTime?)field._value;
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator DateTime(DbfField field) => field._value is not null ? (DateTime)field._value : throw new InvalidCastException();

    /// <summary>
    /// Performs an implicit conversion from <see cref="string"/> to <see cref="DbfField"/>.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator DbfField(string? text) => new() { _value = text };
    /// <summary>
    /// Performs an implicit conversion from <see cref="DbfField" /> to <see cref="string" />.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator string?(DbfField field) => (string?)field._value;
}
