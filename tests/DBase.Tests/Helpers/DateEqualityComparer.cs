using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests.Helpers;

internal sealed class DateEqualityComparer : IEqualityComparer<byte[]>
{
    public static DateEqualityComparer Instance => field ??= new DateEqualityComparer();

    public bool Equals(byte[]? x, byte[]? y) =>
        DbfFieldDateFormatter.ReadRaw(x, Encoding.UTF8) == DbfFieldDateFormatter.ReadRaw(y, Encoding.UTF8);

    public int GetHashCode(byte[]? obj) =>
        DbfFieldDateFormatter.ReadRaw(obj, Encoding.UTF8).GetHashCode();
}
