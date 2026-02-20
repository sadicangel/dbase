using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests.Helpers;

internal sealed class VariantEqualityComparer : IEqualityComparer<byte[]>
{
    public static VariantEqualityComparer Instance => field ??= new VariantEqualityComparer();

    public bool Equals(byte[]? x, byte[]? y) =>
        DbfFieldVariantFormatter.ReadRaw(x, Encoding.UTF8) == DbfFieldVariantFormatter.ReadRaw(y, Encoding.UTF8);

    public int GetHashCode(byte[]? obj) =>
        DbfFieldVariantFormatter.ReadRaw(obj, Encoding.UTF8).GetHashCode();
}
