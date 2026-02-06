using System.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldVariantFormatter
{
    public static string ReadRaw(ReadOnlySpan<byte> source, Encoding encoding)
        => encoding.GetString(source[..source[^1]]);

    public static void WriteRaw(Span<byte> target, ReadOnlySpan<char> value, Encoding encoding)
    {
        if (value.Length is 0)
        {
            target.Fill((byte)' ');
            return;
        }

        _ = encoding.TryGetBytes(value, target[..^1], out var bytesWritten);
        target[^1] = (byte)bytesWritten;
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DbfField)value!).GetValue<string>(), context.Encoding);
        }

        if (propertyType == typeof(string))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (string?)value, context.Encoding);
        }

        if (propertyType == typeof(char[]))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding).ToCharArray();

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (char[]?)value, context.Encoding);
        }

        if (propertyType == typeof(ReadOnlyMemory<char>))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding).AsMemory();

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding);
        }

        throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(propertyType));
    }
}
