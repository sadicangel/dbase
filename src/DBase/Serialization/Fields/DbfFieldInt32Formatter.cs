using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldInt32Formatter
{
    public static int ReadRaw(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadInt32LittleEndian(source);

    public static void WriteRaw(Span<byte> target, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(target, value);

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<int>());
        }

        if (propertyType == typeof(int))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (int)value!);
        }

        if (propertyType == typeof(uint))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                unchecked((uint)ReadRaw(source));

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, unchecked((int)(uint)value!));
        }

        throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(propertyType));
    }
}
