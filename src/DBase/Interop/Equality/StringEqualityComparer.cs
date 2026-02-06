using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Interop.Equality;

internal sealed class StringEqualityComparer(Encoding encoding) : DbfFieldEqualityComparer
{
    public override bool Equals(byte[]? x, byte[]? y)
    {
        var xString = DbfFieldCharacterFormatter.ReadRaw(x, encoding).Trim();
        var yString = DbfFieldCharacterFormatter.ReadRaw(y, encoding).Trim();
        return xString == yString;
    }

    public override int GetHashCode(byte[]? obj) =>
        DbfFieldCharacterFormatter.ReadRaw(obj, encoding).Trim().GetHashCode();
}
