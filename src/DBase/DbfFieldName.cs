using System.Runtime.CompilerServices;
using System.Text;

namespace DBase;

/// <summary>
/// Represents a DBF field name stored as a fixed 10-byte ASCII buffer.
/// </summary>
/// <remarks>
/// DBF field descriptors store the name in a 10-byte slot followed by a null terminator in the
/// descriptor structure. This type models the 10-byte name payload directly: input longer than 10 bytes is
/// truncated, and unused bytes remain zero.
/// </remarks>
[InlineArray(Size)]
public struct DbfFieldName : IEquatable<DbfFieldName>, IEquatable<ReadOnlySpan<char>>, IEquatable<ReadOnlySpan<byte>>
{
    internal const int Size = 10;
    private byte _e0;

    /// <summary>
    /// Gets a value indicating whether the name contains non-empty ASCII bytes with optional null padding.
    /// </summary>
    /// <remarks>
    /// This is the validity rule used by this library when creating descriptors.
    /// </remarks>
    public readonly bool IsValid
    {
        get
        {
            var hasName = false;
            var hasTerminator = false;

            for (var i = 0; i < Size; ++i)
            {
                var value = this[i];
                if (value is 0)
                {
                    hasTerminator = true;
                    continue;
                }

                if (hasTerminator || value > 0x7F || char.IsWhiteSpace((char)value))
                {
                    return false;
                }

                hasName = true;
            }

            return hasName;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldName" /> struct.
    /// </summary>
    /// <param name="name">Field-name bytes. Bytes beyond 10 are ignored.</param>
    /// <remarks>
    /// The input is copied as-is (no encoding conversion). Remaining bytes are left as zero.
    /// Descriptor construction rejects names that are empty, contain non-ASCII bytes, or contain
    /// non-zero bytes after the first null terminator.
    /// </remarks>
    public DbfFieldName(ReadOnlySpan<byte> name)
    {
        this = default;
        var length = Math.Min(name.Length, Size);
        for (var i = 0; i < length; ++i)
            this[i] = name[i];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldName" /> struct.
    /// </summary>
    /// <param name="name">Field-name characters to encode as ASCII.</param>
    /// <remarks>
    /// Characters are copied as ASCII bytes. Inputs longer than 10 characters are truncated, and inputs
    /// containing any non-ASCII character produce an invalid name that descriptor construction rejects.
    /// </remarks>
    public DbfFieldName(ReadOnlySpan<char> name)
    {
        this = default;
        _ = TryCopyAscii(name, this);
    }

    /// <summary>
    /// Returns the field name decoded as an ASCII string.
    /// </summary>
    /// <returns>
    /// A string from the first byte up to (but not including) the first <c>0x00</c> byte, or all 10 bytes if
    /// no null byte is present.
    /// </returns>
    public readonly override string ToString()
    {
        ReadOnlySpan<byte> span = this;
        var length = span.IndexOf((byte)0);
        length = length == -1 ? span.Length : length;
        return Encoding.ASCII.GetString(span[..length]);
    }

    /// <inheritdoc />
    public readonly bool Equals(DbfFieldName other) => ((ReadOnlySpan<byte>)this).SequenceEqual(other);

    /// <inheritdoc />
    public readonly override bool Equals(object? obj) => obj is DbfFieldName name && Equals(name);

    /// <inheritdoc />
    public readonly override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var @byte in this)
            hash.Add(@byte);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether this name matches the specified raw-byte representation.
    /// </summary>
    /// <param name="other">Bytes to compare against.</param>
    /// <returns><see langword="true"/> if all bytes are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(ReadOnlySpan<byte> other) => ((ReadOnlySpan<byte>)this).SequenceEqual(other);

    /// <summary>
    /// Determines whether this name matches the specified character representation.
    /// </summary>
    /// <param name="other">Characters to compare against.</param>
    /// <returns><see langword="true"/> if the encoded bytes match; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(ReadOnlySpan<char> other)
    {
        Span<byte> buffer = stackalloc byte[Size];
        return TryCopyAscii(other, buffer)
            && Equals(buffer);
    }

    private static bool TryCopyAscii(ReadOnlySpan<char> source, Span<byte> destination)
    {
        destination.Clear();
        for (var i = 0; i < source.Length; ++i)
        {
            if (!char.IsAscii(source[i]))
            {
                return false;
            }
        }

        var length = Math.Min(source.Length, Size);
        for (var i = 0; i < length; ++i)
            destination[i] = (byte)source[i];

        return true;
    }

    /// <summary>Determines whether two specified <see cref="DbfFieldName"/> values are equal.</summary>
    public static bool operator ==(DbfFieldName left, DbfFieldName right) => left.Equals(right);

    /// <summary>Determines whether two specified <see cref="DbfFieldName"/> values are not equal.</summary>
    public static bool operator !=(DbfFieldName left, DbfFieldName right) => !(left == right);

    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> to a <see cref="DbfFieldName"/>.
    /// </summary>
    public static implicit operator DbfFieldName(ReadOnlySpan<byte> name) => new(name);

    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> to a <see cref="DbfFieldName"/>.
    /// </summary>
    public static implicit operator DbfFieldName(ReadOnlySpan<char> name) => new(name);
}
