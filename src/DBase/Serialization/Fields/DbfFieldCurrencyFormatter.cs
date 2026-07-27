using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldCurrencyFormatter
{
    public static decimal ReadRaw(ReadOnlySpan<byte> source) => decimal.FromOACurrency(BinaryPrimitives.ReadInt64LittleEndian(source));

    public static void WriteRaw(Span<byte> target, decimal value) => BinaryPrimitives.WriteInt64LittleEndian(target, decimal.ToOACurrency(value));

    private static void WriteRaw(Span<byte> target, decimal? value)
    {
        if (value is null)
        {
            throw new InvalidOperationException("Currency fields cannot serialize null values because the field data does not carry an inline null marker.");
        }

        WriteRaw(target, value.Value);
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, in DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<decimal>());
        }

        if (propertyType == typeof(decimal))
        {
            return new DbfFieldFormatter(Read, Write);

            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, in DbfSerializationContext _) =>
                WriteRaw(target, (decimal)value!);
        }

        if (propertyType == typeof(decimal?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, in DbfSerializationContext _) =>
                WriteRaw(target, (decimal?)value);
        }

        throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(propertyType));
    }
}
