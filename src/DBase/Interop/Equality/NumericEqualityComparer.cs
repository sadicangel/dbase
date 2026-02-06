using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Interop.Equality;

internal sealed class NumericEqualityComparer : DbfFieldEqualityComparer
{
    public static NumericEqualityComparer Instance => field ??= new NumericEqualityComparer();

    public override bool Equals(byte[]? x, byte[]? y)
    {
        var xd = DbfFieldNumericFormatter.ReadRaw(ReplaceCommas(x), Encoding.UTF8, '.');
        var yd = DbfFieldNumericFormatter.ReadRaw(ReplaceCommas(y), Encoding.UTF8, '.');
        if (xd is null) return yd is null;
        if (yd is null) return false;
        return Math.Abs(xd.Value - yd.Value) <= 1e-15;
    }

    public override int GetHashCode(byte[]? obj) =>
        DbfFieldNumericFormatter.ReadRaw(ReplaceCommas(obj), Encoding.UTF8, '.').GetHashCode();

    private static byte[]? ReplaceCommas(byte[]? obj)
    {
        if (obj is null) return null;
        obj.Replace((byte)',', (byte)'.');
        return obj;
    }
}
