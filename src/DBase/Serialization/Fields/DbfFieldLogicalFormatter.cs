using System.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldLogicalFormatter
{
    public static bool? ReadRaw(ReadOnlySpan<byte> source, Encoding encoding)
    {
        if (encoding.GetCharCount(source) is not 1) return null;
        Span<char> v = ['\0'];
        encoding.GetChars(source, v);
        return char.ToUpperInvariant(v[0]) switch
        {
            '?' or ' ' => null,
            'T' or 'Y' or '1' => true,
            'F' or 'N' or '0' => false,
            // TODO: Maybe we should just return null?
            _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
        };
    }

    public static void WriteRaw(Span<byte> target, bool? value)
        => target[0] = value is null ? (byte)'?' : value.Value ? (byte)'T' : (byte)'F';

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<bool?>());
        }

        if (propertyType == typeof(bool) || propertyType == typeof(bool?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (bool?)value);
        }

        throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(propertyType));
    }
}
