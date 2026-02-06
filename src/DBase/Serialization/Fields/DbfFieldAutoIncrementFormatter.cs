using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldAutoIncrementFormatter
{
    public static long ReadRaw(ReadOnlySpan<byte> source) => BinaryPrimitives.ReadInt64LittleEndian(source);

    public static void WriteRaw(Span<byte> target, long value) => BinaryPrimitives.WriteInt64LittleEndian(target, value);

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);
            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) => (DbfField)ReadRaw(source);
            static void Write(Span<byte> source, object? value, DbfSerializationContext _) => WriteRaw(source, ((DbfField)value!).GetValue<long>());
        }

        if (propertyType == typeof(int))
        {
            return new DbfFieldFormatter(Read, Write);
            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) => (int)ReadRaw(source);
            static void Write(Span<byte> source, object? value, DbfSerializationContext _) => WriteRaw(source, (int)value!);
        }

        if (propertyType == typeof(long))
        {
            return new DbfFieldFormatter(Read, Write);
            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) => ReadRaw(source);
            static void Write(Span<byte> target, object? value, DbfSerializationContext _) => WriteRaw(target, (long)value!);
        }

        if (propertyType == typeof(ulong))
        {
            return new DbfFieldFormatter(Read, Write);
            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) => unchecked((ulong)ReadRaw(source));
            static void Write(Span<byte> target, object? value, DbfSerializationContext _) => WriteRaw(target, unchecked((long)(ulong)value!));
        }

        throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(propertyType));
    }
}
