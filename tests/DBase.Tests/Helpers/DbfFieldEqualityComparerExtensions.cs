using System.Text;

namespace DBase.Tests.Helpers;

internal static class DbfFieldEqualityComparerExtensions
{
    public static IEqualityComparer<byte[]> GetRawEqualityComparer(this DbfFieldDescriptor descriptor, Encoding encoding) => descriptor.Type switch
    {
        DbfFieldType.AutoIncrement => BinaryEqualityComparer.Instance,
        DbfFieldType.Binary => BinaryEqualityComparer.Instance,
        DbfFieldType.Blob => BinaryEqualityComparer.Instance,
        DbfFieldType.Character => new StringEqualityComparer(encoding),
        DbfFieldType.Currency => BinaryEqualityComparer.Instance,
        DbfFieldType.Date => DateEqualityComparer.Instance,
        DbfFieldType.DateTime => BinaryEqualityComparer.Instance,
        DbfFieldType.Double => BinaryEqualityComparer.Instance,
        DbfFieldType.Float => NumericEqualityComparer.Instance,
        DbfFieldType.Int32 => BinaryEqualityComparer.Instance,
        DbfFieldType.Logical => LogicalEqualityComparer.Instance,
        DbfFieldType.Memo => BinaryEqualityComparer.Instance,
        DbfFieldType.NullFlags => BinaryEqualityComparer.Instance,
        DbfFieldType.Numeric => NumericEqualityComparer.Instance,
        DbfFieldType.Ole => BinaryEqualityComparer.Instance,
        DbfFieldType.Picture => BinaryEqualityComparer.Instance,
        DbfFieldType.Timestamp => BinaryEqualityComparer.Instance,
        DbfFieldType.Variant => VariantEqualityComparer.Instance,
        _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor, null)
    };
}
