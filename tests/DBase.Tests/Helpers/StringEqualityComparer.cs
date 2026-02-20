using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests.Helpers;

internal sealed class StringEqualityComparer(Encoding encoding) : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    {
        var xString = DbfFieldCharacterFormatter.ReadRaw(x, encoding).Trim();
        var yString = DbfFieldCharacterFormatter.ReadRaw(y, encoding).Trim();
        return xString == yString;
    }

    public int GetHashCode(byte[]? obj) =>
        DbfFieldCharacterFormatter.ReadRaw(obj, encoding).Trim().GetHashCode();
}
