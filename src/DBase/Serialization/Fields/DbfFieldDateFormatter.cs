using System.Globalization;
using System.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldDateFormatter
{
    public static DateTime? ReadRaw(ReadOnlySpan<byte> source, Encoding encoding)
    {
        source = source.Trim("\0 "u8);
        if (source.Length != 8) return null;
        Span<char> date = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source[..8], date);
        return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    public static void WriteRaw(Span<byte> target, DateTime? value, Encoding encoding)
    {
        if (value is null)
        {
            target.Fill((byte)' ');
            return;
        }

        var dateTime = value.Value;
        Span<char> chars = stackalloc char[8];
        if (!dateTime.TryFormat(chars, out _, "yyyyMMdd", CultureInfo.InvariantCulture) ||
            !encoding.TryGetBytes(chars, target, out _))
        {
            target.Fill((byte)' ');
        }
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DbfField)value!).GetValue<DateTime?>(), context.Encoding);
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding);

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (DateTime?)value, context.Encoding);
        }

        if (propertyType == typeof(DateOnly) || propertyType == typeof(DateOnly?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context)
            {
                var dt = ReadRaw(source, context.Encoding);
                return dt is null ? null : DateOnly.FromDateTime(dt.Value);
            }

            static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue), context.Encoding);
        }

        throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(propertyType));
    }
}
