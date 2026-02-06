using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldDoubleFormatter
{
    public static double ReadRaw(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadDoubleLittleEndian(source);

    public static void WriteRaw(Span<byte> target, double value)
        => BinaryPrimitives.WriteDoubleLittleEndian(target, value);

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<double>());
        }

        if (propertyType == typeof(double))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (double)value!);
        }

        throw new ArgumentException("Double fields must be of a type convertible to double", nameof(propertyType));
    }
}
