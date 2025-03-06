using System.Runtime.CompilerServices;
using System.Text;

namespace DBase;

/// <summary>
/// Represents the name of a field in a <see cref="DbfRecord" />.
/// </summary>
[InlineArray(Size)]
public struct DbfFieldName : IEquatable<DbfFieldName>, IEquatable<ReadOnlySpan<char>>, IEquatable<ReadOnlySpan<byte>>
{
    internal const int Size = 10;
    private byte _e0;

    /// <summary>
    /// Gets a value indicating whether the field name is valid.
    /// </summary>
    public readonly bool IsValid
    {
        get
        {
            for (var i = 0; i < Size; ++i)
                if (!char.IsWhiteSpace((char)this[i]))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldName" /> struct.
    /// </summary>
    /// <param name="name">The field name.</param>
    public DbfFieldName(ReadOnlySpan<byte> name)
    {
        var length = Math.Min(name.Length, Size);
        for (var i = 0; i < length; ++i)
            this[i] = name[i];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldName" /> struct.
    /// </summary>
    /// <param name="name">The field name.</param>
    public DbfFieldName(ReadOnlySpan<char> name)
    {
        for (var length = Math.Min(name.Length, 10); length > 0; length--)
            if (Encoding.ASCII.TryGetBytes(name[..length], this, out _))
                break;
    }

    /// <summary>
    /// Returns a string representation of the field name.
    /// </summary>
    /// <returns>A <see cref="string"/> that represents the field name.</returns>
    public override readonly string ToString()
    {
        ReadOnlySpan<byte> span = this;
        var length = span.IndexOf((byte)0);
        length = length == -1 ? span.Length : length;
        return Encoding.ASCII.GetString(span[..length]);
    }

    /// <inheritdoc />
    public readonly bool Equals(DbfFieldName other) => ((ReadOnlySpan<byte>)this).SequenceEqual(other);

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is DbfFieldName name && Equals(name);

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var @byte in this)
            hash.Add(@byte);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public readonly bool Equals(ReadOnlySpan<byte> other) => ((ReadOnlySpan<byte>)this).SequenceEqual(other);

    /// <inheritdoc />
    public readonly bool Equals(ReadOnlySpan<char> other)
    {
        Span<byte> buffer = stackalloc byte[Size];
        return Encoding.ASCII.TryGetBytes(other, buffer, out var bytesWritten)
            && bytesWritten <= Size
            && ((ReadOnlySpan<byte>)this).SequenceEqual(buffer);
    }

    /// <inheritdoc />
    public static bool operator ==(DbfFieldName left, DbfFieldName right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(DbfFieldName left, DbfFieldName right) => !(left == right);

    /// <inheritdoc />
    public static implicit operator DbfFieldName(ReadOnlySpan<byte> name) => new(name);

    /// <inheritdoc />
    public static implicit operator DbfFieldName(ReadOnlySpan<char> name) => new(name);
}
