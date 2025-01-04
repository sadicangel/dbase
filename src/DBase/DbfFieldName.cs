using System.Runtime.CompilerServices;
using System.Text;

namespace DBase;

[InlineArray(Size)]
public struct DbfFieldName : IEquatable<DbfFieldName>
{
    internal const int Size = 10;

#pragma warning disable IDE0051 // Remove unused private members
    private byte _e0;
#pragma warning restore IDE0051 // Remove unused private members

    public readonly bool IsValid
    {
        get
        {
            for (var i = 0; i < Size; ++i)
                if (char.IsWhiteSpace((char)this[i]))
                    return true;
            return false;
        }
    }

    public DbfFieldName(ReadOnlySpan<byte> name)
    {
        var length = Math.Min(name.Length, Size);
        for (var i = 0; i < length; ++i)
            this[i] = name[i];
    }

    public DbfFieldName(ReadOnlySpan<char> name)
    {
        for (var length = Math.Min(name.Length, 10); length > 0; length--)
            if (Encoding.ASCII.TryGetBytes(name[..length], this, out _))
                break;
    }

    public override readonly string ToString()
    {
        ReadOnlySpan<byte> span = this;
        var length = span.IndexOf((byte)0);
        length = length == -1 ? span.Length : length;
        return Encoding.ASCII.GetString(span[..length]);
    }

    public readonly bool Equals(DbfFieldName other) => ((ReadOnlySpan<byte>)this).SequenceEqual(other);

    public override readonly bool Equals(object? obj) => obj is DbfFieldName name && Equals(name);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var @byte in this)
            hash.Add(@byte);
        return hash.ToHashCode();
    }

    public static bool operator ==(DbfFieldName left, DbfFieldName right) => left.Equals(right);

    public static bool operator !=(DbfFieldName left, DbfFieldName right) => !(left == right);
}
