using System.Collections.Concurrent;
using System.Text;
using DBase.Interop;

namespace DBase.Serialization;

internal delegate void SerializeField(Span<byte> target, object? value, Encoding encoding, char decimalSeparator, Memo? memo);

internal static partial class DbfFieldSerializer
{
    private readonly record struct SerializerKey(Type PropertyType, DbfFieldType DbfFieldType, byte Length, byte Decimal);

    private static readonly ConcurrentDictionary<SerializerKey, SerializeField> s_serializers = [];

    public static SerializeField GetSerializer(Type propertyType, in DbfFieldDescriptor fieldDescriptor) =>
        s_serializers.GetOrAdd(new SerializerKey(propertyType, fieldDescriptor.Type, fieldDescriptor.Length, fieldDescriptor.Decimal), CreateSerializer);

    private static SerializeField CreateSerializer(SerializerKey key)
    {
        return key.DbfFieldType switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementSerializer(key),
            DbfFieldType.Binary => GetBinarySerializer(key),
            DbfFieldType.Blob => GetBlobSerializer(key),
            DbfFieldType.Character => GetCharacterSerializer(key),
            DbfFieldType.Currency => GetCurrencySerializer(key),
            DbfFieldType.Date => GetDateSerializer(key),
            DbfFieldType.DateTime => GetDateTimeSerializer(key),
            DbfFieldType.Double => GetDoubleSerializer(key),
            DbfFieldType.Float => GetNumericSerializer(key),
            DbfFieldType.Int32 => GetInt32Serializer(key),
            DbfFieldType.Logical => GetLogicalSerializer(key),
            DbfFieldType.Memo => GetMemoSerializer(key),
            DbfFieldType.NullFlags => GetNullFlagsSerializer(key),
            DbfFieldType.Numeric => GetNumericSerializer(key),
            DbfFieldType.Ole => GetOleSerializer(key),
            DbfFieldType.Picture => GetPictureSerializer(key),
            DbfFieldType.Timestamp => GetTimestampSerializer(key),
            DbfFieldType.Variant => GetVariantSerializer(key),
            _ => throw new NotImplementedException(),
        };
    }

    private static SerializeField GetAutoIncrementSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(long))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteAutoIncrement(target, (long)value!);
        }

        if (key.PropertyType == typeof(ulong))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteAutoIncrement(target, unchecked((long)(ulong)value!));
        }

        throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(key));
    }

    private static SerializeField GetBinarySerializer(SerializerKey key)
    {
        if (key.Length is 8)
        {
            return GetDoubleSerializer(key);
        }

        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBinary(target, (string?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBinary(target, (char[]?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBinary(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
        }

        throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetBlobSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBlob(target, (string?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBlob(target, (char[]?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoBlob(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
        }

        throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetCharacterSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteCharacter(target, (string?)value, encoding);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteCharacter(target, (char[]?)value, encoding);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteCharacter(target, ((ReadOnlyMemory<char>)value!).Span, encoding);
        }

        throw new ArgumentException("Character fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetCurrencySerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(decimal))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteCurrency(target, (decimal)value!);
        }

        throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(key));
    }

    private static SerializeField GetDateSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(DateTime) || key.PropertyType == typeof(DateTime?))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteDate(target, (DateTime?)value, encoding);
        }

        if (key.PropertyType == typeof(DateTimeOffset) || key.PropertyType == typeof(DateTimeOffset?))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteDate(target, ((DateTimeOffset?)value)?.DateTime, encoding);
        }

        if (key.PropertyType == typeof(DateOnly) || key.PropertyType == typeof(DateOnly?))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteDate(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue), encoding);
        }

        throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(key));
    }

    private static SerializeField GetDateTimeSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(DateTime) || key.PropertyType == typeof(DateTime?))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteDateTime(target, (DateTime?)value);
        }

        if (key.PropertyType == typeof(DateTimeOffset) || key.PropertyType == typeof(DateTimeOffset?))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteDateTime(target, ((DateTimeOffset?)value)?.DateTime);
        }

        if (key.PropertyType == typeof(DateOnly) || key.PropertyType == typeof(DateOnly?))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteDateTime(target, ((DateOnly?)value)?.ToDateTime(TimeOnly.MinValue));
        }

        throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(key));
    }

    private static SerializeField GetDoubleSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(double))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteDouble(target, (double)value!);
        }

        throw new ArgumentException("Double fields must be of a type convertible to double", nameof(key));
    }

    private static SerializeField GetInt32Serializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(int))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteInt32(target, (int)value!);
        }

        if (key.PropertyType == typeof(uint))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteInt32(target, unchecked((int)(uint)value!));
        }

        throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(key));
    }

    private static SerializeField GetLogicalSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(bool) || key.PropertyType == typeof(bool?))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteLogical(target, (bool?)value);
        }

        throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(key));
    }

    private static SerializeField GetMemoSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoString(target, (string?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoString(target, (char[]?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoString(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
        }

        throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetNullFlagsSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, _, _, _) =>
                DbfMarshal.WriteNullFlags(target, (string?)value);
        }

        // TODO: Support integers and flag enums.
        throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetNumericSerializer(SerializerKey key)
    {
        if (key.Decimal is 0)
        {
            // TODO: Support other integer types.
            if (key.PropertyType == typeof(int) || key.PropertyType == typeof(int?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteNumericInteger(target, (int?)value, encoding);
            }

            if (key.PropertyType == typeof(uint) || key.PropertyType == typeof(uint?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteNumericInteger(target, (uint?)value, encoding);
            }

            if (key.PropertyType == typeof(long) || key.PropertyType == typeof(long?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteNumericInteger(target, (long?)value, encoding);
            }

            if (key.PropertyType == typeof(ulong) || key.PropertyType == typeof(ulong?))
            {
                return static (target, value, encoding, _, _) =>
                    DbfMarshal.WriteNumericInteger(target, value is null ? null : unchecked((long)(ulong)value), encoding);
            }
        }

        if (key.PropertyType == typeof(double) || key.PropertyType == typeof(double?))
        {
            return (target, value, encoding, decimalSeparator, _) =>
                DbfMarshal.WriteNumericFloat(target, (double?)value, key.Decimal, encoding, decimalSeparator);
        }

        if (key.PropertyType == typeof(float) || key.PropertyType == typeof(float?))
        {
            return (target, value, encoding, decimalSeparator, _) =>
                DbfMarshal.WriteNumericFloat(target, (float?)value, key.Decimal, encoding, decimalSeparator);
        }

        throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(key));
    }

    private static SerializeField GetOleSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoOle(target, (string?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoOle(target, (char[]?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoOle(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
        }

        throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetPictureSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoPicture(target, (string?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoPicture(target, (char[]?)value, encoding, memo);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, memo) =>
                DbfMarshal.WriteMemoPicture(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
        }

        throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(key));
    }

    private static SerializeField GetTimestampSerializer(SerializerKey key)
        => GetDateTimeSerializer(key);

    private static SerializeField GetVariantSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteVariant(target, (string?)value, encoding);
        }

        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteVariant(target, (char[]?)value, encoding);
        }

        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, encoding, _, _) =>
                DbfMarshal.WriteVariant(target, ((ReadOnlyMemory<char>)value!).Span, encoding);
        }

        throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(key));
    }
}
