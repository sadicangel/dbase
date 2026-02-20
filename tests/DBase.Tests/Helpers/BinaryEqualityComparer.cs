namespace DBase.Tests.Helpers;

internal sealed class BinaryEqualityComparer : IEqualityComparer<byte[]>
{
    public static BinaryEqualityComparer Instance => field ??= new BinaryEqualityComparer();

    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        if (x is { Length: not 8 } && y is { Length: not 8 }) return true;

        return x.SequenceEqual(y);
    }

    public int GetHashCode(byte[]? obj) => obj?.GetHashCode() ?? 0;
}
