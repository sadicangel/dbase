﻿using System.Diagnostics;
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
public readonly struct DbfField : IEquatable<DbfField>
{
    private readonly object? _value;

    /// <summary>
    /// Gets the value of the field.
    /// </summary>
    public readonly object? Value => _value;

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_value))]
    public readonly bool IsNull => _value is null;

    private DbfField(object? value) => _value = value;

    /// <summary>
    /// Determines whether this instance is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to check.</typeparam>
    /// <returns>
    ///   <see langword="true" /> if this instance is of type <typeparamref name="T"/>; otherwise, <see langword="false" />.
    /// </returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public readonly bool IsType<T>() => _value is not null && typeof(T) == _value.GetType();

    /// <summary>
    /// Gets the value of the field as type <typeparamref name="T"/> or <see langword="null" /> if the field is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The type to get the value as.</typeparam>
    /// <returns>The value of the field as type <typeparamref name="T"/> or <see langword="null" /> if the field is <see langword="null" />.</returns>
    public readonly T? GetValue<T>() => _value is T value ? value : default;


    /// <summary>
    /// Gets the string representation of the field.
    /// </summary>
    /// <returns>The string representation of the field.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>
    /// Gets the string representation of the field using the specified format provider.
    /// </summary>
    /// <param name="provider">The format provider to use.</param>
    /// <returns>The string representation of the field.</returns>
    public readonly string ToString(IFormatProvider? provider) => string.Format(provider, "{0}", _value);

    private readonly string GetDebuggerDisplay() => ToString();

    /// <inheritdoc/>
    public readonly bool Equals(DbfField other) => _value is null ? other._value is null : _value.Equals(other._value);

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is DbfField field && Equals(field);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => HashCode.Combine(_value);

    /// <inheritdoc/>
    public static bool operator ==(DbfField left, DbfField right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(DbfField left, DbfField right) => !(left == right);


    /// <inheritdoc/>
    public static implicit operator DbfField(bool boolean) => new(boolean);
    /// <inheritdoc/>
    public static implicit operator DbfField(bool? boolean) => new(boolean);
    /// <inheritdoc/>
    public static explicit operator bool?(DbfField field) => (bool?)field._value;
    /// <inheritdoc/>
    public static explicit operator bool(DbfField field) => field._value is bool value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(int number) => new(number);
    /// <inheritdoc/>
    public static implicit operator DbfField(int? number) => new(number);
    /// <inheritdoc/>
    public static explicit operator int?(DbfField field) => (int?)field._value;
    /// <inheritdoc/>
    public static explicit operator int(DbfField field) => field._value is int value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(long number) => new(number);
    /// <inheritdoc/>
    public static implicit operator DbfField(long? number) => new(number);
    /// <inheritdoc/>
    public static explicit operator long?(DbfField field) => (long?)field._value;
    /// <inheritdoc/>
    public static explicit operator long(DbfField field) => field._value is long value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(double number) => new(number);
    /// <inheritdoc/>
    public static implicit operator DbfField(double? number) => new(number);
    /// <inheritdoc/>
    public static explicit operator double?(DbfField field) => (double?)field._value;
    /// <inheritdoc/>
    public static explicit operator double(DbfField field) => field._value is double value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(decimal number) => new(number);
    /// <inheritdoc/>
    public static implicit operator DbfField(decimal? number) => new(number);
    /// <inheritdoc/>
    public static explicit operator decimal?(DbfField field) => (decimal?)field._value;
    /// <inheritdoc/>
    public static explicit operator decimal(DbfField field) => field._value is decimal value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(DateTime dateTime) => new(dateTime);
    /// <inheritdoc/>
    public static implicit operator DbfField(DateTime? dateTime) => new(dateTime);
    /// <inheritdoc/>
    public static explicit operator DateTime?(DbfField field) => (DateTime?)field._value;
    /// <inheritdoc/>
    public static explicit operator DateTime(DbfField field) => field._value is DateTime value ? value : throw new InvalidCastException();

    /// <inheritdoc/>
    public static implicit operator DbfField(string? text) => new(text);
    /// <inheritdoc/>
    public static explicit operator string?(DbfField field) => (string?)field._value;
}
