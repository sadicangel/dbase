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

        Span<char> format = stackalloc char[4];
        format[0] = 'F';
        if (!@decimal.TryFormat(format[1..], out var charsWritten))
            throw new InvalidOperationException("Failed to create decimal format");
        format = format[..(1 + charsWritten)];

        var capacity = Math.Max(target.Length, 32);
        Span<char> chars = stackalloc char[Math.Min(capacity, 256)];
        if (capacity > chars.Length)
        {
            chars = new char[capacity];
        }

        if (!f64.TryFormat(chars, out charsWritten, format, CultureInfo.InvariantCulture))
            throw new OverflowException($"Numeric value '{f64}' does not fit in field length {target.Length} with {@decimal} decimal place(s).");

        chars = chars[..charsWritten];

        if (decimalSeparator is not '.' && chars.IndexOf('.') is var idx and >= 0)
            chars[idx] = decimalSeparator;

        WriteRightAligned(target, chars, encoding);
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
        WriteRightAligned(target, @long[..charsWritten], encoding);
    }

    public static ulong? ReadRawUnsigned(ReadOnlySpan<byte> source, Encoding encoding)
    {
        source = source.Trim("\0 "u8);
        if (source.IsEmpty) return null;
        Span<char> integer = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, integer);
        return ulong.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    public static void WriteRaw(Span<byte> target, ulong? value, Encoding encoding)
    {
        target.Fill((byte)' ');
        if (value is null)
            return;

        var u64 = value.Value;

        Span<char> @ulong = stackalloc char[20];
        if (!u64.TryFormat(@ulong, out var charsWritten, "D", CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Failed to format value '{u64}' as '{DbfFieldType.Numeric}'");
        WriteRightAligned(target, @ulong[..charsWritten], encoding);
    }

    private static void WriteRightAligned(Span<byte> target, ReadOnlySpan<char> value, Encoding encoding)
    {
        var byteCount = encoding.GetByteCount(value);
        if (byteCount > target.Length)
        {
            throw new OverflowException($"Numeric value '{new string(value)}' requires {byteCount} byte(s), but field length is {target.Length}.");
        }

        _ = encoding.GetBytes(value, target[^byteCount..]);
    }

    public static DbfFieldFormatter Create(Type propertyType, byte @decimal)
    {
        if (@decimal is 0)
        {
            if (propertyType == typeof(DbfField))
            {
                return new DbfFieldFormatter(Read, Write);

                static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    (DbfField)ReadRaw(source, context.Encoding);

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, ((DbfField)value!).GetValue<long?>(), context.Encoding);
            }

            if (propertyType == typeof(int))
            {
                return new DbfFieldFormatter(Read, Write);

                static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
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

                static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRawUnsigned(source, context.Encoding) is { } l ? checked((uint)l) : 0U;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (ulong?)(uint?)value, context.Encoding);
            }

            if (propertyType == typeof(uint?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRawUnsigned(source, context.Encoding) is { } l ? checked((uint)l) : null;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (ulong?)(uint?)value, context.Encoding);
            }

            if (propertyType == typeof(long))
            {
                return new DbfFieldFormatter(Read, Write);

                static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
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

                static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRawUnsigned(source, context.Encoding) ?? 0UL;

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (ulong?)value, context.Encoding);
            }

            if (propertyType == typeof(ulong?))
            {
                return new DbfFieldFormatter(Read, Write);

                static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                    ReadRawUnsigned(source, context.Encoding);

                static void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                    WriteRaw(target, (ulong?)value, context.Encoding);
            }
        }

        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadRaw(source, context.Encoding, context.DecimalSeparator);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteRaw(target, ((DbfField)value!).GetValue<double?>(), @decimal, context.Encoding, context.DecimalSeparator);
        }

        if (propertyType == typeof(float))
        {
            return new DbfFieldFormatter(Read, Write);

            static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
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

            static object Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
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
