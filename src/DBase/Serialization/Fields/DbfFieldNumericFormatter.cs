using System.Globalization;
using System.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldNumericFormatter
{
    public static double? ReadRaw(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator)
    {
        source = source.Trim("\0 "u8);
        if (source.IsEmpty || (source.Length == 1 && !char.IsAsciiDigit((char)source[0])))
            return null;
        Span<char> @double = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, @double);
        if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
            @double[idx] = '.';
        return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    public static long? ReadRaw(ReadOnlySpan<byte> source, Encoding encoding)
    {
        source = source.Trim("\0 "u8);
        if (source.IsEmpty) return null;
        Span<char> integer = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, integer);
        return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    public static void WriteRaw(Span<byte> target, double? value, byte @decimal, Encoding encoding, char decimalSeparator)
    {
        target.Fill((byte)' ');
        if (value is null)
            return;

        var f64 = value.Value;

        Span<char> format = ['F', '\0', '\0'];
        if (!@decimal.TryFormat(format[1..], out var charsWritten))
            throw new InvalidOperationException("Failed to create decimal format");
        format = format[..(1 + charsWritten)];

        Span<char> chars = stackalloc char[20];
        if (!f64.TryFormat(chars, out charsWritten, format, CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Failed to format value '{f64}' for numeric field");

        if (decimalSeparator is not '.' && chars.IndexOf(decimalSeparator) is var idx and >= 0)
            chars[idx] = decimalSeparator;

        _ = encoding.TryGetBytes(chars[..charsWritten], target[^charsWritten..], out _);
    }

    public static void WriteRaw(Span<byte> target, long? value, Encoding encoding)
    {
        target.Fill((byte)' ');
        if (value is null)
            return;

        var i64 = value.Value;

        Span<char> @long = stackalloc char[20];
        if (!i64.TryFormat(@long, out var charsWritten, "D", CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Failed to format value '{i64}' as '{DbfFieldType.Numeric}'");
        _ = encoding.TryGetBytes(@long[..charsWritten], target, out _);
    }

    public static DbfFieldFormatter Create(Type propertyType, byte @decimal)
    {
        if (@decimal is 0)
        {
            if (propertyType == typeof(DbfField))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    (DbfField)ReadRaw(source, context.Encoding);

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, ((DbfField)value!).GetValue<long?>(), context.Encoding);
            }

            if (propertyType == typeof(int))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? (int)l : 0;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (int?)value, context.Encoding);
            }

            if (propertyType == typeof(int?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? (int)l : null;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (int?)value, context.Encoding);
            }

            if (propertyType == typeof(uint))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? unchecked((uint)l) : 0U;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (uint?)value, context.Encoding);
            }

            if (propertyType == typeof(uint?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? unchecked((uint)l) : null;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (uint?)value, context.Encoding);
            }

            if (propertyType == typeof(long))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) ?? 0L;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (long?)value, context.Encoding);
            }

            if (propertyType == typeof(long?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding);

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (long?)value, context.Encoding);
            }

            if (propertyType == typeof(ulong))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? unchecked((ulong)l) : 0UL;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, unchecked((long?)(ulong?)value), context.Encoding);
            }

            if (propertyType == typeof(ulong?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRaw(source, context.Encoding) is { } l ? unchecked((ulong)l) : null;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, unchecked((long?)(ulong?)value), context.Encoding);
            }
        }

        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source, context.Encoding, context.DecimalSeparator);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DbfField)value!).GetValue<double?>(), @decimal, context.Encoding, context.DecimalSeparator);
        }

        if (propertyType == typeof(float))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding, context.DecimalSeparator) is { } d ? (float)d : 0f;

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (float?)value, @decimal, context.Encoding, context.DecimalSeparator);
        }

        if (propertyType == typeof(float?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding, context.DecimalSeparator) is { } d ? (float)d : null;

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (float?)value, @decimal, context.Encoding, context.DecimalSeparator);
        }

        if (propertyType == typeof(double))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding, context.DecimalSeparator) ?? 0D;

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (double?)value, @decimal, context.Encoding, context.DecimalSeparator);
        }

        if (propertyType == typeof(double?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadRaw(source, context.Encoding, context.DecimalSeparator);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, (double?)value, @decimal, context.Encoding, context.DecimalSeparator);
        }

        throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(propertyType));
    }
}
