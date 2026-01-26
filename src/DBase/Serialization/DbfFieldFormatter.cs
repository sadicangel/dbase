using System.Reflection;
using DBase.Interop;

namespace DBase.Serialization;

internal readonly struct DbfFieldFormatter(PropertyInfo property, DbfFieldDescriptor descriptor)
{
    private delegate object? ReadValue(ReadOnlySpan<byte> source, DbfSerializationContext context);

    private delegate void WriteValue(Span<byte> target, object? value, DbfSerializationContext context);

    private readonly ReadValue _read = CreateReader(property, descriptor);
    private readonly WriteValue _write = CreateWriter(property, descriptor);

    public object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) => _read(source, context);

    public void Write(Span<byte> target, object? value, DbfSerializationContext context) => _write(target, value, context);

    private static ReadValue CreateReader(PropertyInfo property, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementReader(property, descriptor),
            DbfFieldType.Binary => GetBinaryReader(property, descriptor),
            DbfFieldType.Blob => GetBlobReader(property, descriptor),
            DbfFieldType.Character => GetCharacterReader(property, descriptor),
            DbfFieldType.Currency => GetCurrencyReader(property, descriptor),
            DbfFieldType.Date => GetDateReader(property, descriptor),
            DbfFieldType.DateTime => GetDateTimeReader(property, descriptor),
            DbfFieldType.Double => GetDoubleReader(property, descriptor),
            DbfFieldType.Float => GetNumericReader(property, descriptor),
            DbfFieldType.Int32 => GetInt32Reader(property, descriptor),
            DbfFieldType.Logical => GetLogicalReader(property, descriptor),
            DbfFieldType.Memo => GetMemoReader(property, descriptor),
            DbfFieldType.NullFlags => GetNullFlagsReader(property, descriptor),
            DbfFieldType.Numeric => GetNumericReader(property, descriptor),
            DbfFieldType.Ole => GetOleReader(property, descriptor),
            DbfFieldType.Picture => GetPictureReader(property, descriptor),
            DbfFieldType.Timestamp => GetTimestampReader(property, descriptor),
            DbfFieldType.Variant => GetVariantReader(property, descriptor),
            _ => throw new NotImplementedException(),
        };

        static ReadValue GetAutoIncrementReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(long))
            {
                return static (source, _) =>
                    DbfMarshal.ReadAutoIncrement(source);
            }

            if (property.PropertyType == typeof(ulong))
            {
                return static (source, _) =>
                    unchecked((ulong)DbfMarshal.ReadAutoIncrement(source));
            }

            throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(property));
        }

        static ReadValue GetBinaryReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Length is 8)
            {
                return GetDoubleReader(property, descriptor);
            }

            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBinary(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBinary(source, context.Encoding, context.Memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBinary(source, context.Encoding, context.Memo).AsMemory();
            }

            throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetBlobReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBlob(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBlob(source, context.Encoding, context.Memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoBlob(source, context.Encoding, context.Memo).AsMemory();
            }

            throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetCharacterReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadCharacter(source, context.Encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadCharacter(source, context.Encoding).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadCharacter(source, context.Encoding).AsMemory();
            }

            throw new ArgumentException("Character fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetCurrencyReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(decimal))
            {
                return static (source, _) =>
                    DbfMarshal.ReadCurrency(source);
            }

            throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(property));
        }

        static ReadValue GetDateReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding) ?? default;
            }

            if (property.PropertyType == typeof(DateTime?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding);
            }

            if (property.PropertyType == typeof(DateTimeOffset))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding) is { } dateTime ? new DateTimeOffset(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding) is { } dateTime ? new DateTimeOffset(dateTime) : null;
            }

            if (property.PropertyType == typeof(DateOnly))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding) is { } dateTime ? DateOnly.FromDateTime(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateOnly?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadDate(source, context.Encoding) is { } dateTime ? DateOnly.FromDateTime(dateTime) : null;
            }

            throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(property));
        }

        static ReadValue GetDateTimeReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source) ?? default;
            }

            if (property.PropertyType == typeof(DateTime?))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source);
            }

            if (property.PropertyType == typeof(DateTimeOffset))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? new DateTimeOffset(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? new DateTimeOffset(dateTime) : null;
            }

            if (property.PropertyType == typeof(DateOnly))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? DateOnly.FromDateTime(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateOnly?))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? DateOnly.FromDateTime(dateTime) : null;
            }

            throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(property));
        }

        static ReadValue GetDoubleReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(double))
            {
                return static (source, _) =>
                    DbfMarshal.ReadDouble(source);
            }

            throw new ArgumentException("Double fields must be of a type convertible to double", nameof(property));
        }

        static ReadValue GetInt32Reader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(int))
            {
                return static (source, _) =>
                    DbfMarshal.ReadInt32(source);
            }

            if (property.PropertyType == typeof(uint))
            {
                return static (source, _) =>
                    unchecked((uint)DbfMarshal.ReadInt32(source));
            }

            throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(property));
        }

        static ReadValue GetLogicalReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(bool))
            {
                return static (source, context) =>
                    DbfMarshal.ReadLogical(source, context.Encoding) ?? false;
            }

            if (property.PropertyType == typeof(bool?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadLogical(source, context.Encoding);
            }

            throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(property));
        }

        static ReadValue GetMemoReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoString(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoString(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoString(source, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetNullFlagsReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, _) =>
                    DbfMarshal.ReadNullFlags(source);
            }

            // TODO: Support integers and flag enums.
            throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetNumericReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Decimal is 0)
            {
                if (property.PropertyType == typeof(int))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? (int)l : 0;
                }

                if (property.PropertyType == typeof(int?))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? (int)l : null;
                }

                if (property.PropertyType == typeof(uint))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? unchecked((uint)l) : 0U;
                }

                if (property.PropertyType == typeof(uint?))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? unchecked((uint)l) : null;
                }

                if (property.PropertyType == typeof(long))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) ?? 0L;
                }

                if (property.PropertyType == typeof(long?))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding);
                }

                if (property.PropertyType == typeof(ulong))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? unchecked((ulong)l) : 0UL;
                }

                if (property.PropertyType == typeof(ulong?))
                {
                    return static (source, context) =>
                        DbfMarshal.ReadNumericInteger(source, context.Encoding) is { } l ? unchecked((ulong)l) : null;
                }
            }

            if (property.PropertyType == typeof(float))
            {
                return static (source, context) =>
                    DbfMarshal.ReadNumericDouble(source, context.Encoding, context.DecimalSeparator) is { } d ? (float)d : 0f;
            }

            if (property.PropertyType == typeof(float?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadNumericDouble(source, context.Encoding, context.DecimalSeparator) is { } d ? (float)d : null;
            }

            if (property.PropertyType == typeof(double))
            {
                return static (source, context) =>
                    DbfMarshal.ReadNumericDouble(source, context.Encoding, context.DecimalSeparator) ?? 0D;
            }

            if (property.PropertyType == typeof(double?))
            {
                return static (source, context) =>
                    DbfMarshal.ReadNumericDouble(source, context.Encoding, context.DecimalSeparator);
            }

            throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(property));
        }

        static ReadValue GetOleReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoOle(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoOle(source, context.Encoding, context.Memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoOle(source, context.Encoding, context.Memo).AsMemory();
            }

            throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetPictureReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoPicture(source, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoPicture(source, context.Encoding, context.Memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadMemoPicture(source, context.Encoding, context.Memo).AsMemory();
            }

            throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetTimestampReader(PropertyInfo property, DbfFieldDescriptor descriptor)
            => GetDateTimeReader(property, descriptor);

        static ReadValue GetVariantReader(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, context) =>
                    DbfMarshal.ReadVariant(source, context.Encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, context) =>
                    DbfMarshal.ReadVariant(source, context.Encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, context) =>
                    DbfMarshal.ReadVariant(source, context.Encoding);
            }

            throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(property));
        }
    }

    private static WriteValue CreateWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementWriter(property, descriptor),
            DbfFieldType.Binary => GetBinaryWriter(property, descriptor),
            DbfFieldType.Blob => GetBlobWriter(property, descriptor),
            DbfFieldType.Character => GetCharacterWriter(property, descriptor),
            DbfFieldType.Currency => GetCurrencyWriter(property, descriptor),
            DbfFieldType.Date => GetDateWriter(property, descriptor),
            DbfFieldType.DateTime => GetDateTimeWriter(property, descriptor),
            DbfFieldType.Double => GetDoubleWriter(property, descriptor),
            DbfFieldType.Float => GetNumericWriter(property, descriptor),
            DbfFieldType.Int32 => GetInt32Writer(property, descriptor),
            DbfFieldType.Logical => GetLogicalWriter(property, descriptor),
            DbfFieldType.Memo => GetMemoWriter(property, descriptor),
            DbfFieldType.NullFlags => GetNullFlagsWriter(property, descriptor),
            DbfFieldType.Numeric => GetNumericWriter(property, descriptor),
            DbfFieldType.Ole => GetOleWriter(property, descriptor),
            DbfFieldType.Picture => GetPictureWriter(property, descriptor),
            DbfFieldType.Timestamp => GetTimestampWriter(property, descriptor),
            DbfFieldType.Variant => GetVariantWriter(property, descriptor),
            _ => throw new NotImplementedException(),
        };

        static WriteValue GetAutoIncrementWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(long))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteAutoIncrement(target, (long)value!);
            }

            if (property.PropertyType == typeof(ulong))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteAutoIncrement(target, unchecked((long)(ulong)value!));
            }

            throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(property));
        }

        static WriteValue GetBinaryWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Length is 8)
            {
                return GetDoubleWriter(property, descriptor);
            }

            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBinary(target, (string?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBinary(target, (char[]?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBinary(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetBlobWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBlob(target, (string?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBlob(target, (char[]?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoBlob(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetCharacterWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteCharacter(target, (string?)value, context.Encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteCharacter(target, (char[]?)value, context.Encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteCharacter(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding);
            }

            throw new ArgumentException("Character fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetCurrencyWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(decimal))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteCurrency(target, (decimal)value!);
            }

            throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(property));
        }

        static WriteValue GetDateWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteDate(target, (DateTime?)value, context.Encoding);
            }

            if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteDate(target, ((DateTimeOffset?)value)?.DateTime, context.Encoding);
            }

            if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteDate(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue), context.Encoding);
            }

            throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(property));
        }

        static WriteValue GetDateTimeWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteDateTime(target, (DateTime?)value);
            }

            if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteDateTime(target, ((DateTimeOffset?)value)?.DateTime);
            }

            if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteDateTime(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue));
            }

            throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(property));
        }

        static WriteValue GetDoubleWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(double))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteDouble(target, (double)value!);
            }

            throw new ArgumentException("Double fields must be of a type convertible to double", nameof(property));
        }

        static WriteValue GetInt32Writer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(int))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteInt32(target, (int)value!);
            }

            if (property.PropertyType == typeof(uint))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteInt32(target, unchecked((int)(uint)value!));
            }

            throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(property));
        }

        static WriteValue GetLogicalWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteLogical(target, (bool?)value);
            }

            throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(property));
        }

        static WriteValue GetMemoWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoString(target, (string?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoString(target, (char[]?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoString(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetNullFlagsWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, _) =>
                    DbfMarshal.WriteNullFlags(target, (string?)value);
            }

            // TODO: Support integers and flag enums.
            throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetNumericWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Decimal is 0)
            {
                // TODO: Support other integer types.
                if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    return static (target, value, context) =>
                        DbfMarshal.WriteNumericInteger(target, (int?)value, context.Encoding);
                }

                if (property.PropertyType == typeof(uint) || property.PropertyType == typeof(uint?))
                {
                    return static (target, value, context) =>
                        DbfMarshal.WriteNumericInteger(target, (uint?)value, context.Encoding);
                }

                if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
                {
                    return static (target, value, context) =>
                        DbfMarshal.WriteNumericInteger(target, (long?)value, context.Encoding);
                }

                if (property.PropertyType == typeof(ulong) || property.PropertyType == typeof(ulong?))
                {
                    return static (target, value, context) =>
                        DbfMarshal.WriteNumericInteger(target, value is null ? null : unchecked((long)(ulong)value), context.Encoding);
                }
            }

            if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
            {
                return (target, value, context) =>
                    DbfMarshal.WriteNumericFloat(target, (double?)value, descriptor.Decimal, context.Encoding, context.DecimalSeparator);
            }

            if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                return (target, value, context) =>
                    DbfMarshal.WriteNumericFloat(target, (float?)value, descriptor.Decimal, context.Encoding, context.DecimalSeparator);
            }

            throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(property));
        }

        static WriteValue GetOleWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoOle(target, (string?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoOle(target, (char[]?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoOle(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetPictureWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoPicture(target, (string?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoPicture(target, (char[]?)value, context.Encoding, context.Memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteMemoPicture(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
            }

            throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetTimestampWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
            => GetDateTimeWriter(property, descriptor);

        static WriteValue GetVariantWriter(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteVariant(target, (string?)value, context.Encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteVariant(target, (char[]?)value, context.Encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, context) =>
                    DbfMarshal.WriteVariant(target, ((ReadOnlyMemory<char>)value!).Span, context.Encoding);
            }

            throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(property));
        }
    }
}
