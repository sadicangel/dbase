using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DotNext.Buffers;
using DotNext.Buffers.Text;
using DotNext.Text;

namespace DBase.Serialization;

internal delegate object? ReadValue(ReadOnlySpan<byte> source, DbfSerializationContext context);

internal delegate void WriteValue(Span<byte> target, object? value, DbfSerializationContext context);

internal readonly struct DbfFieldFormatter(ReadValue read, WriteValue write)
{
    public object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) => read(source, context);

    public void Write(Span<byte> target, object? value, DbfSerializationContext context) => write(target, value, context);

    public static DbfFieldFormatter Create(Type propertyType, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => DbfFieldAutoIncrementFormatter.Create(propertyType),
            DbfFieldType.Binary when descriptor.Length == 8 => DbfFieldDoubleFormatter.Create(propertyType),
            DbfFieldType.Binary => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Blob => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Character => DbfFieldCharacterFormatter.Create(propertyType),
            DbfFieldType.Currency => DbfFieldCurrencyFormatter.Create(propertyType),
            DbfFieldType.Date => DbfFieldDateFormatter.Create(propertyType),
            DbfFieldType.DateTime => DbfFieldDateTimeFormatter.Create(propertyType),
            DbfFieldType.Double => DbfFieldDoubleFormatter.Create(propertyType),
            DbfFieldType.Float => DbfFieldNumericFormatter.Create(propertyType, descriptor.Decimal),
            DbfFieldType.Int32 => DbfFieldInt32Formatter.Create(propertyType),
            DbfFieldType.Logical => DbfFieldLogicalFormatter.Create(propertyType),
            DbfFieldType.Memo => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Memo),
            DbfFieldType.NullFlags => DbfFieldNullFlagsFormatter.Create(propertyType),
            DbfFieldType.Numeric => DbfFieldNumericFormatter.Create(propertyType, descriptor.Decimal),
            DbfFieldType.Ole => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Picture => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Picture),
            DbfFieldType.Timestamp => DbfFieldDateTimeFormatter.Create(propertyType),
            DbfFieldType.Variant => DbfFieldVariantFormatter.Create(propertyType),
            _ => throw new NotSupportedException($"Field type '{descriptor.Type}' is not supported.")
        };
    }
}

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

internal static class DbfFieldCharacterFormatter
{
    public static string ReadRaw(ReadOnlySpan<byte> source, Encoding encoding) => encoding.GetString(source.Trim("\0 "u8));

    public static void WriteRaw(Span<byte> target, ReadOnlySpan<char> value, Encoding encoding)
    {
        target.Fill((byte)' ');
        if (value.IsEmpty)
            return;
        _ = encoding.TryGetBytes(value, target, out _);
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

        throw new ArgumentException("Character fields must be of a type convertible to string", nameof(propertyType));
    }
}

internal static class DbfFieldCurrencyFormatter
{
    public static decimal ReadRaw(ReadOnlySpan<byte> source) => decimal.FromOACurrency(BinaryPrimitives.ReadInt64LittleEndian(source));

    public static void WriteRaw(Span<byte> target, decimal value) => BinaryPrimitives.WriteInt64LittleEndian(target, decimal.ToOACurrency(value));

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<decimal>());
        }

        if (propertyType == typeof(decimal))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (decimal)value!);
        }

        throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(propertyType));
    }
}

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

internal static class DbfFieldDateTimeFormatter
{
    public static DateTime? ReadRaw(ReadOnlySpan<byte> source)
    {
        var julian = BinaryPrimitives.ReadInt32LittleEndian(source);
        if (julian is 0) return null;
        var milliseconds = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4, 4));
        return DateTime.FromOADate(julian - 2415018.5).AddMilliseconds(milliseconds);
    }

    public static void WriteRaw(Span<byte> target, DateTime? value)
    {
        if (value is null)
        {
            target.Clear();
            return;
        }

        var dateTime = value.Value;
        var julian = (int)(dateTime.Date.ToOADate() + 2415018.5);
        BinaryPrimitives.WriteInt32LittleEndian(target, julian);
        var milliseconds = 43200000 /* 12h */ + (int)dateTime.TimeOfDay.TotalMilliseconds;
        BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4, 4), milliseconds);
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<DateTime?>());
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (DateTime?)value);
        }

        if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _)
            {
                var dt = ReadRaw(source);
                return dt is null ? null : new DateTimeOffset(dt.Value);
            }

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DateTimeOffset?)value)?.DateTime);
        }

        throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(propertyType));
    }
}

internal static class DbfFieldDoubleFormatter
{
    public static double ReadRaw(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadDoubleLittleEndian(source);

    public static void WriteRaw(Span<byte> target, double value)
        => BinaryPrimitives.WriteDoubleLittleEndian(target, value);

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

        throw new ArgumentException("Double fields must be of a type convertible to double", nameof(propertyType));
    }
}

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

        _ = encoding.TryGetBytes(chars[..charsWritten], target, out _);
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

internal static class DbfFieldMemoFormatter
{
    private static string ReadMemo(ReadOnlySpan<byte> source, MemoRecordType type, Encoding encoding, Memo? memo)
    {
        if (memo is null || source is [])
            return string.Empty;

        int index;
        if (source.Length is 4)
        {
            index = BinaryPrimitives.ReadInt32LittleEndian(source);
        }
        else
        {
            Span<char> chars = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, chars);
            chars = chars.Trim();
            if (chars is [])
                return string.Empty;
            index = int.Parse(chars);
        }

        if (index == 0)
            return string.Empty;

        var writer = new BufferWriterSlim<byte>(memo.BlockLength);

        try
        {
            memo.Get(index, out _, ref writer);

            var data = type is MemoRecordType.Memo
                ? encoding.GetString(writer.WrittenSpan)
                : Convert.ToBase64String(writer.WrittenSpan);

            return data;
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void WriteMemo(Span<byte> target, MemoRecordType type, ReadOnlySpan<char> value, Encoding encoding, Memo? memo)
    {
        target.Fill(target.Length is 4 ? (byte)0 : (byte)' ');
        if (memo is null || value.Length is 0)
            return;

        var index = memo.NextIndex;

        if (target.Length is 4)
        {
            BinaryPrimitives.WriteInt32LittleEndian(target, index);
        }
        else
        {
            Span<char> chars = stackalloc char[10];
            index.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
            var bytesRequired = encoding.GetByteCount(chars[..charsWritten]);
            encoding.TryGetBytes(chars[..charsWritten], target[Math.Max(0, 10 - bytesRequired)..], out _);
        }

        using var data = type is MemoRecordType.Memo
            ? encoding.GetBytes(value)
            : new Base64Decoder().DecodeFromUtf16(value);

        memo.Add(type, data.Span);
    }

    public static DbfFieldFormatter Create(Type propertyType, MemoRecordType recordType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadMemo(source, recordType, context.Encoding, context.Memo);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, ((DbfField)value!).GetValue<string>(), context.Encoding, context.Memo);
        }

        if (propertyType == typeof(string))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, (string?)value, context.Encoding, context.Memo);
        }

        if (propertyType == typeof(char[]))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo).ToCharArray();

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, (char[]?)value, context.Encoding, context.Memo);
        }

        if (propertyType == typeof(ReadOnlyMemory<char>))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo).AsMemory();

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
        }

        throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(propertyType));
    }
}

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
