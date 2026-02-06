namespace DBase.Serialization.Fields;

internal static class DbfFieldNullFlagsFormatter
{
    public static string ReadRaw(ReadOnlySpan<byte> source)
        => Convert.ToHexString(source);

    public static void WriteRaw(Span<byte> target, ReadOnlySpan<char> value)
    {
        if (value.Length is 0)
        {
            target.Clear();
            return;
        }

        Convert.FromHexString(value, target, out _, out _);
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DbfField)value!).GetValue<string>());
        }

        if (propertyType == typeof(string))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (string?)value);
        }

        throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(propertyType));
    }
}
