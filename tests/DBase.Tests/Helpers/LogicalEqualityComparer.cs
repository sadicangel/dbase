using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests.Helpers;

internal sealed class LogicalEqualityComparer : IEqualityComparer<byte[]>
{
    public static LogicalEqualityComparer Instance => field ??= new LogicalEqualityComparer();

    public bool Equals(byte[]? x, byte[]? y) =>
        DbfFieldLogicalFormatter.ReadRaw(x, Encoding.UTF8) == DbfFieldLogicalFormatter.ReadRaw(y, Encoding.UTF8);

    public int GetHashCode(byte[]? obj) =>
        DbfFieldLogicalFormatter.ReadRaw(obj, Encoding.UTF8).GetHashCode();
}
