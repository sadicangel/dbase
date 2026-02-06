namespace DBase.Interop.Equality;

internal sealed class BinaryEqualityComparer : DbfFieldEqualityComparer
{
    public static BinaryEqualityComparer Instance => field ??= new BinaryEqualityComparer();

    public override bool Equals(byte[]? x, byte[]? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        if (x is { Length: not 8 } && y is { Length: not 8 }) return true;

        return x.SequenceEqual(y);
    }

    public override int GetHashCode(byte[]? obj) => obj?.GetHashCode() ?? 0;
}
