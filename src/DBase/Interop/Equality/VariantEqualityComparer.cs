using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Interop.Equality;

internal sealed class VariantEqualityComparer : DbfFieldEqualityComparer
{
    public static VariantEqualityComparer Instance => field ??= new VariantEqualityComparer();

    public override bool Equals(byte[]? x, byte[]? y) =>
        DbfFieldVariantFormatter.ReadRaw(x, Encoding.UTF8) == DbfFieldVariantFormatter.ReadRaw(y, Encoding.UTF8);

    public override int GetHashCode(byte[]? obj) =>
        DbfFieldVariantFormatter.ReadRaw(obj, Encoding.UTF8).GetHashCode();
}
