using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Interop.Equality;

internal sealed class LogicalEqualityComparer : DbfFieldEqualityComparer
{
    public static LogicalEqualityComparer Instance => field ??= new LogicalEqualityComparer();

    public override bool Equals(byte[]? x, byte[]? y) =>
        DbfFieldLogicalFormatter.ReadRaw(x, Encoding.UTF8) == DbfFieldLogicalFormatter.ReadRaw(y, Encoding.UTF8);

    public override int GetHashCode(byte[]? obj) =>
        DbfFieldLogicalFormatter.ReadRaw(obj, Encoding.UTF8).GetHashCode();
}
