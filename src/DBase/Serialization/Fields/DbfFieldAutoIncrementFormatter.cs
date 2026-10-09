using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldAutoIncrementFormatter
{
    public static int ReadRaw(ReadOnlySpan<byte> source) => BinaryPrimitives.ReadInt32LittleEndian(source);

    public static void WriteRaw(Span<byte> target, int value) => BinaryPrimitives.WriteInt32LittleEndian(target, value);

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);
            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) => (DbfField)ReadRaw(source);
            static void Write(Span<byte> source, object? value, in DbfSerializationContext _) => WriteRaw(source, ConvertToInt32((DbfField)value!));
        }

        if (propertyType == typeof(int))
        {
            return new DbfFieldFormatter(Read, Write);
            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) => ReadRaw(source);
            static void Write(Span<byte> source, object? value, in DbfSerializationContext _) => WriteRaw(source, (int)value!);
        }

        if (propertyType == typeof(uint))
        {
            return new DbfFieldFormatter(Read, Write);
            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) => checked((uint)ReadRaw(source));
            static void Write(Span<byte> source, object? value, in DbfSerializationContext _) => WriteRaw(source, ConvertToInt32((uint)value!));
        }

        if (propertyType == typeof(long))
        {
            return new DbfFieldFormatter(Read, Write);
            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) => (long)ReadRaw(source);
            static void Write(Span<byte> target, object? value, in DbfSerializationContext _) => WriteRaw(target, ConvertToInt32((long)value!));
        }

        if (propertyType == typeof(ulong))
        {
            return new DbfFieldFormatter(Read, Write);
            static object Read(ReadOnlySpan<byte> source, in DbfSerializationContext _) => checked((ulong)ReadRaw(source));
            static void Write(Span<byte> target, object? value, in DbfSerializationContext _) => WriteRaw(target, ConvertToInt32((ulong)value!));
        }

        throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int32", nameof(propertyType));
    }

    internal static int ConvertToInt32(DbfField value) => value switch
    {
        null => 0,
        int i32 => i32,
        long i64 => ConvertToInt32(i64),
        _ => throw new InvalidCastException("AutoIncrement field values must be convertible to Int32.")
    };

    private static int ConvertToInt32(long value)
    {
        if (value is < int.MinValue or > int.MaxValue)
        {
            throw new OverflowException($"AutoIncrement value '{value}' does not fit in a 4-byte signed integer field.");
        }

        return (int)value;
    }

    private static int ConvertToInt32(ulong value)
    {
        if (value > int.MaxValue)
        {
            throw new OverflowException($"AutoIncrement value '{value}' does not fit in a 4-byte signed integer field.");
        }

        return (int)value;
    }
}
