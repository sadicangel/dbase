using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldCurrencyFormatter
{
    public static decimal ReadRaw(ReadOnlySpan<byte> source) => decimal.FromOACurrency(BinaryPrimitives.ReadInt64LittleEndian(source));

    public static void WriteRaw(Span<byte> target, decimal value) => BinaryPrimitives.WriteInt64LittleEndian(target, decimal.ToOACurrency(value));

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<decimal>());
        }

        if (propertyType == typeof(decimal))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (decimal)value!);
        }

        throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(propertyType));
    }
}
