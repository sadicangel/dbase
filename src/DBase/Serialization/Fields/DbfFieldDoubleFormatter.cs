using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldDoubleFormatter
{
    public static double ReadRaw(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadDoubleLittleEndian(source);

    public static void WriteRaw(Span<byte> target, double value)
        => BinaryPrimitives.WriteDoubleLittleEndian(target, value);

    private static byte[] ReadBytes(ReadOnlySpan<byte> source) => source.ToArray();

    private static void WriteBytes(Span<byte> target, byte[]? value)
    {
        target.Clear();
        if (value is null)
            return;

        value.CopyTo(target);
    }

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

        if (propertyType == typeof(byte[]))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadBytes(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteBytes(target, (byte[]?)value);
        }

        throw new ArgumentException("Double fields must be of a type convertible to double", nameof(propertyType));
    }
}
