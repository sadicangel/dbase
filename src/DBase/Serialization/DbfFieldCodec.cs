using System.Reflection;
using System.Text;
using DBase.Interop;

namespace DBase.Serialization;

internal readonly struct DbfFieldCodec(PropertyInfo property, DbfFieldDescriptor descriptor)
{
    private delegate object? ReadValue(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator, Memo? memo);

    private delegate void WriteValue(Span<byte> target, object? value, Encoding encoding, char decimalSeparator, Memo? memo);

    private readonly ReadValue _read = CreateDeserializer(property, descriptor);
    private readonly WriteValue _write = CreateSerializer(property, descriptor);

    public object? Read(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator, Memo? memo) =>
        _read(source, encoding, decimalSeparator, memo);

    public void Write(Span<byte> target, object? value, Encoding encoding, char decimalSeparator, Memo? memo) =>
        _write(target, value, encoding, decimalSeparator, memo);

    private static ReadValue CreateDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementDeserializer(property, descriptor),
            DbfFieldType.Binary => GetBinaryDeserializer(property, descriptor),
            DbfFieldType.Blob => GetBlobDeserializer(property, descriptor),
            DbfFieldType.Character => GetCharacterDeserializer(property, descriptor),
            DbfFieldType.Currency => GetCurrencyDeserializer(property, descriptor),
            DbfFieldType.Date => GetDateDeserializer(property, descriptor),
            DbfFieldType.DateTime => GetDateTimeDeserializer(property, descriptor),
            DbfFieldType.Double => GetDoubleDeserializer(property, descriptor),
            DbfFieldType.Float => GetNumericDeserializer(property, descriptor),
            DbfFieldType.Int32 => GetInt32Deserializer(property, descriptor),
            DbfFieldType.Logical => GetLogicalDeserializer(property, descriptor),
            DbfFieldType.Memo => GetMemoDeserializer(property, descriptor),
            DbfFieldType.NullFlags => GetNullFlagsDeserializer(property, descriptor),
            DbfFieldType.Numeric => GetNumericDeserializer(property, descriptor),
            DbfFieldType.Ole => GetOleDeserializer(property, descriptor),
            DbfFieldType.Picture => GetPictureDeserializer(property, descriptor),
            DbfFieldType.Timestamp => GetTimestampDeserializer(property, descriptor),
            DbfFieldType.Variant => GetVariantDeserializer(property, descriptor),
            _ => throw new NotImplementedException(),
        };

        static ReadValue GetAutoIncrementDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(long))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadAutoIncrement(source);
            }

            if (property.PropertyType == typeof(ulong))
            {
                return static (source, _, _, _) =>
                    unchecked((ulong)DbfMarshal.ReadAutoIncrement(source));
            }

            throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(property));
        }

        static ReadValue GetBinaryDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Length is 8)
            {
                return GetDoubleDeserializer(property, descriptor);
            }

            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBinary(source, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBinary(source, encoding, memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBinary(source, encoding, memo).AsMemory();
            }

            throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetBlobDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBlob(source, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBlob(source, encoding, memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoBlob(source, encoding, memo).AsMemory();
            }

            throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetCharacterDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadCharacter(source, encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadCharacter(source, encoding).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadCharacter(source, encoding).AsMemory();
            }

            throw new ArgumentException("Character fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetCurrencyDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(decimal))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadCurrency(source);
            }

            throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(property));
        }

        static ReadValue GetDateDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding) ?? default;
            }

            if (property.PropertyType == typeof(DateTime?))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding);
            }

            if (property.PropertyType == typeof(DateTimeOffset))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding) is { } dateTime ? new DateTimeOffset(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding) is { } dateTime ? new DateTimeOffset(dateTime) : null;
            }

            if (property.PropertyType == typeof(DateOnly))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding) is { } dateTime ? DateOnly.FromDateTime(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateOnly?))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadDate(source, encoding) is { } dateTime ? DateOnly.FromDateTime(dateTime) : null;
            }

            throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(property));
        }

        static ReadValue GetDateTimeDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source) ?? default;
            }

            if (property.PropertyType == typeof(DateTime?))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source);
            }

            if (property.PropertyType == typeof(DateTimeOffset))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? new DateTimeOffset(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? new DateTimeOffset(dateTime) : null;
            }

            if (property.PropertyType == typeof(DateOnly))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? DateOnly.FromDateTime(dateTime) : default;
            }

            if (property.PropertyType == typeof(DateOnly?))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDateTime(source) is { } dateTime ? DateOnly.FromDateTime(dateTime) : null;
            }

            throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(property));
        }

        static ReadValue GetDoubleDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(double))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadDouble(source);
            }

            throw new ArgumentException("Double fields must be of a type convertible to double", nameof(property));
        }

        static ReadValue GetInt32Deserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(int))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadInt32(source);
            }

            if (property.PropertyType == typeof(uint))
            {
                return static (source, _, _, _) =>
                    unchecked((uint)DbfMarshal.ReadInt32(source));
            }

            throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(property));
        }

        static ReadValue GetLogicalDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(bool))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadLogical(source, encoding) ?? false;
            }

            if (property.PropertyType == typeof(bool?))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadLogical(source, encoding);
            }

            throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(property));
        }

        static ReadValue GetMemoDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoString(source, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoString(source, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoString(source, encoding, memo);
            }

            throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetNullFlagsDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, _, _, _) =>
                    DbfMarshal.ReadNullFlags(source);
            }

            // TODO: Support integers and flag enums.
            throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetNumericDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Decimal is 0)
            {
                if (property.PropertyType == typeof(int))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? (int)l : 0;
                }

                if (property.PropertyType == typeof(int?))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? (int)l : null;
                }

                if (property.PropertyType == typeof(uint))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? unchecked((uint)l) : 0U;
                }

                if (property.PropertyType == typeof(uint?))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? unchecked((uint)l) : null;
                }

                if (property.PropertyType == typeof(long))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) ?? 0L;
                }

                if (property.PropertyType == typeof(long?))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding);
                }

                if (property.PropertyType == typeof(ulong))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? unchecked((ulong)l) : 0UL;
                }

                if (property.PropertyType == typeof(ulong?))
                {
                    return static (source, encoding, _, _) =>
                        DbfMarshal.ReadNumericInteger(source, encoding) is { } l ? unchecked((ulong)l) : null;
                }
            }

            if (property.PropertyType == typeof(float))
            {
                return static (source, encoding, decimalSeparator, _) =>
                    DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) is { } d ? (float)d : 0f;
            }

            if (property.PropertyType == typeof(float?))
            {
                return static (source, encoding, decimalSeparator, _) =>
                    DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) is { } d ? (float)d : null;
            }

            if (property.PropertyType == typeof(double))
            {
                return static (source, encoding, decimalSeparator, _) =>
                    DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) ?? 0D;
            }

            if (property.PropertyType == typeof(double?))
            {
                return static (source, encoding, decimalSeparator, _) =>
                    DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator);
            }

            throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(property));
        }

        static ReadValue GetOleDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoOle(source, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoOle(source, encoding, memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoOle(source, encoding, memo).AsMemory();
            }

            throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetPictureDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoPicture(source, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoPicture(source, encoding, memo).ToCharArray();
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, memo) =>
                    DbfMarshal.ReadMemoPicture(source, encoding, memo).AsMemory();
            }

            throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(property));
        }

        static ReadValue GetTimestampDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
            => GetDateTimeDeserializer(property, descriptor);

        static ReadValue GetVariantDeserializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadVariant(source, encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadVariant(source, encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (source, encoding, _, _) =>
                    DbfMarshal.ReadVariant(source, encoding);
            }

            throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(property));
        }
    }

    private static WriteValue CreateSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementSerializer(property, descriptor),
            DbfFieldType.Binary => GetBinarySerializer(property, descriptor),
            DbfFieldType.Blob => GetBlobSerializer(property, descriptor),
            DbfFieldType.Character => GetCharacterSerializer(property, descriptor),
            DbfFieldType.Currency => GetCurrencySerializer(property, descriptor),
            DbfFieldType.Date => GetDateSerializer(property, descriptor),
            DbfFieldType.DateTime => GetDateTimeSerializer(property, descriptor),
            DbfFieldType.Double => GetDoubleSerializer(property, descriptor),
            DbfFieldType.Float => GetNumericSerializer(property, descriptor),
            DbfFieldType.Int32 => GetInt32Serializer(property, descriptor),
            DbfFieldType.Logical => GetLogicalSerializer(property, descriptor),
            DbfFieldType.Memo => GetMemoSerializer(property, descriptor),
            DbfFieldType.NullFlags => GetNullFlagsSerializer(property, descriptor),
            DbfFieldType.Numeric => GetNumericSerializer(property, descriptor),
            DbfFieldType.Ole => GetOleSerializer(property, descriptor),
            DbfFieldType.Picture => GetPictureSerializer(property, descriptor),
            DbfFieldType.Timestamp => GetTimestampSerializer(property, descriptor),
            DbfFieldType.Variant => GetVariantSerializer(property, descriptor),
            _ => throw new NotImplementedException(),
        };

        static WriteValue GetAutoIncrementSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(long))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteAutoIncrement(target, (long)value!);
            }

            if (property.PropertyType == typeof(ulong))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteAutoIncrement(target, unchecked((long)(ulong)value!));
            }

            throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(property));
        }

        static WriteValue GetBinarySerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Length is 8)
            {
                return GetDoubleSerializer(property, descriptor);
            }

            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBinary(target, (string?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBinary(target, (char[]?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBinary(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetBlobSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBlob(target, (string?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBlob(target, (char[]?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoBlob(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetCharacterSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteCharacter(target, (string?)value, encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteCharacter(target, (char[]?)value, encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteCharacter(target, ((ReadOnlyMemory<char>)value!).Span, encoding);
            }

            throw new ArgumentException("Character fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetCurrencySerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(decimal))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteCurrency(target, (decimal)value!);
            }

            throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(property));
        }

        static WriteValue GetDateSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteDate(target, (DateTime?)value, encoding);
            }

            if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteDate(target, ((DateTimeOffset?)value)?.DateTime, encoding);
            }

            if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteDate(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue), encoding);
            }

            throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(property));
        }

        static WriteValue GetDateTimeSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteDateTime(target, (DateTime?)value);
            }

            if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteDateTime(target, ((DateTimeOffset?)value)?.DateTime);
            }

            if (property.PropertyType == typeof(DateOnly) || property.PropertyType == typeof(DateOnly?))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteDateTime(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue));
            }

            throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(property));
        }

        static WriteValue GetDoubleSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(double))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteDouble(target, (double)value!);
            }

            throw new ArgumentException("Double fields must be of a type convertible to double", nameof(property));
        }

        static WriteValue GetInt32Serializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(int))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteInt32(target, (int)value!);
            }

            if (property.PropertyType == typeof(uint))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteInt32(target, unchecked((int)(uint)value!));
            }

            throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(property));
        }

        static WriteValue GetLogicalSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteLogical(target, (bool?)value);
            }

            throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(property));
        }

        static WriteValue GetMemoSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoString(target, (string?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoString(target, (char[]?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoString(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetNullFlagsSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, _, _, _) =>
                    DbfMarshal.WriteNullFlags(target, (string?)value);
            }

            // TODO: Support integers and flag enums.
            throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetNumericSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (descriptor.Decimal is 0)
            {
                // TODO: Support other integer types.
                if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    return static (target, value, encoding, _, _) =>
                        DbfMarshal.WriteNumericInteger(target, (int?)value, encoding);
                }

                if (property.PropertyType == typeof(uint) || property.PropertyType == typeof(uint?))
                {
                    return static (target, value, encoding, _, _) =>
                        DbfMarshal.WriteNumericInteger(target, (uint?)value, encoding);
                }

                if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
                {
                    return static (target, value, encoding, _, _) =>
                        DbfMarshal.WriteNumericInteger(target, (long?)value, encoding);
                }

                if (property.PropertyType == typeof(ulong) || property.PropertyType == typeof(ulong?))
                {
                    return static (target, value, encoding, _, _) =>
                        DbfMarshal.WriteNumericInteger(target, value is null ? null : unchecked((long)(ulong)value), encoding);
                }
            }

            if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
            {
                return (target, value, encoding, decimalSeparator, _) =>
                    DbfMarshal.WriteNumericFloat(target, (double?)value, descriptor.Decimal, encoding, decimalSeparator);
            }

            if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                return (target, value, encoding, decimalSeparator, _) =>
                    DbfMarshal.WriteNumericFloat(target, (float?)value, descriptor.Decimal, encoding, decimalSeparator);
            }

            throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(property));
        }

        static WriteValue GetOleSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoOle(target, (string?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoOle(target, (char[]?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoOle(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetPictureSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoPicture(target, (string?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoPicture(target, (char[]?)value, encoding, memo);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, memo) =>
                    DbfMarshal.WriteMemoPicture(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(property));
        }

        static WriteValue GetTimestampSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
            => GetDateTimeSerializer(property, descriptor);

        static WriteValue GetVariantSerializer(PropertyInfo property, DbfFieldDescriptor descriptor)
        {
            if (property.PropertyType == typeof(string))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteVariant(target, (string?)value, encoding);
            }

            if (property.PropertyType == typeof(char[]))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteVariant(target, (char[]?)value, encoding);
            }

            if (property.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteVariant(target, ((ReadOnlyMemory<char>)value!).Span, encoding);
            }

            throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(property));
        }
    }
}
