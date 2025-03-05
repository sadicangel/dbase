using System.Collections.Concurrent;
using System.Text;
using DBase.Interop;

namespace DBase.Serialization;

public delegate object? DeserializeField(ReadOnlySpan<byte> source, DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Memo? memo);

internal static partial class DbfFieldSerializer
{
    private readonly record struct DeserializerKey(Type PropertyType, DbfFieldType DbfFieldType, byte Length, byte Decimal);

    private static readonly ConcurrentDictionary<DeserializerKey, DeserializeField> s_deserializers = [];

    public static DeserializeField GetDeserializer(Type propertyType, in DbfFieldDescriptor fieldDescriptor) => s_deserializers.GetOrAdd(new(propertyType, fieldDescriptor.Type, fieldDescriptor.Length, fieldDescriptor.Decimal), CreateDeserializer);

    private static DeserializeField CreateDeserializer(DeserializerKey key)
    {
        return key.DbfFieldType switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementDeserializer(key),
            DbfFieldType.Binary => GetBinaryDeserializer(key),
            DbfFieldType.Blob => GetBlobDeserializer(key),
            DbfFieldType.Character => GetCharacterDeserializer(key),
            DbfFieldType.Currency => GetCurrencyDeserializer(key),
            DbfFieldType.Date => GetDateDeserializer(key),
            DbfFieldType.DateTime => GetDateTimeDeserializer(key),
            DbfFieldType.Double => GetDoubleDeserializer(key),
            DbfFieldType.Float => GetNumericDeserializer(key),
            DbfFieldType.Int32 => GetInt32Deserializer(key),
            DbfFieldType.Logical => GetLogicalDeserializer(key),
            DbfFieldType.Memo => GetMemoDeserializer(key),
            DbfFieldType.NullFlags => GetNullFlagsDeserializer(key),
            DbfFieldType.Numeric => GetNumericDeserializer(key),
            DbfFieldType.Ole => GetOleDeserializer(key),
            DbfFieldType.Picture => GetPictureDeserializer(key),
            DbfFieldType.Timestamp => GetTimestampDeserializer(key),
            DbfFieldType.Variant => GetVariantDeserializer(key),
            _ => throw new NotImplementedException(),
        };
    }

    private static DeserializeField GetAutoIncrementDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(long))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadAutoIncrement(source);
        }
        if (key.PropertyType == typeof(ulong))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                 unchecked((ulong)DbfMarshal.ReadAutoIncrement(source));
        }
        throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(key));
    }

    private static DeserializeField GetBinaryDeserializer(DeserializerKey key)
    {
        if (key.Length is 8)
        {
            return GetDoubleDeserializer(key);
        }
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBinary(source, encoding, memo);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBinary(source, encoding, memo)?.ToCharArray();
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBinary(source, encoding, memo).AsMemory();
        }
        throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetBlobDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBlob(source, encoding, memo);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBlob(source, encoding, memo)?.ToCharArray();
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoBlob(source, encoding, memo).AsMemory();
        }
        throw new ArgumentException("Blob fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetCharacterDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadCharacter(source, encoding);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadCharacter(source, encoding)?.ToCharArray();
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadCharacter(source, encoding).AsMemory();
        }
        throw new ArgumentException("Character fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetCurrencyDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(decimal))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadCurrency(source);
        }
        throw new ArgumentException("Currency fields must be of a type convertible to decimal", nameof(key));
    }

    private static DeserializeField GetDateDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(DateTime))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding) ?? default;
        }
        if (key.PropertyType == typeof(DateTime?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding);
        }
        if (key.PropertyType == typeof(DateTimeOffset))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding) is DateTime dateTime ? new DateTimeOffset(dateTime) : default;
        }
        if (key.PropertyType == typeof(DateTimeOffset?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding) is DateTime dateTime ? new DateTimeOffset(dateTime) : null;
        }
        if (key.PropertyType == typeof(DateOnly))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding) is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : default;
        }
        if (key.PropertyType == typeof(DateOnly?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDate(source, encoding) is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : null;
        }
        throw new ArgumentException("Date fields must be of a type convertible to DateTime", nameof(key));
    }

    private static DeserializeField GetDateTimeDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(DateTime))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source) ?? default;
        }
        if (key.PropertyType == typeof(DateTime?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source);
        }
        if (key.PropertyType == typeof(DateTimeOffset))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source) is DateTime dateTime ? new DateTimeOffset(dateTime) : default;
        }
        if (key.PropertyType == typeof(DateTimeOffset?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source) is DateTime dateTime ? new DateTimeOffset(dateTime) : null;
        }
        if (key.PropertyType == typeof(DateOnly))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source) is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : default;
        }
        if (key.PropertyType == typeof(DateOnly?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDateTime(source) is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : null;
        }
        throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(key));
    }

    private static DeserializeField GetDoubleDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(double))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadDouble(source);
        }
        throw new ArgumentException("Double fields must be of a type convertible to double", nameof(key));
    }

    private static DeserializeField GetInt32Deserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(int))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadInt32(source);
        }
        if (key.PropertyType == typeof(uint))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                unchecked((uint)DbfMarshal.ReadInt32(source));
        }
        throw new ArgumentException("Int32 fields must be of a type convertible to Int32", nameof(key));
    }

    private static DeserializeField GetLogicalDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(bool))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadLogical(source, encoding) ?? false;
        }
        if (key.PropertyType == typeof(bool?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadLogical(source, encoding);
        }
        throw new ArgumentException("Logical fields must be of a type convertible to bool", nameof(key));
    }

    private static DeserializeField GetMemoDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoString(source, encoding, memo);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoString(source, encoding, memo);
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoString(source, encoding, memo);
        }
        throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetNullFlagsDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadNullFlags(source);
        }
        // TODO: Support integers and flag enums.
        throw new ArgumentException("NullFlags fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetNumericDeserializer(DeserializerKey key)
    {
        if (key.Decimal is 0)
        {
            if (key.PropertyType == typeof(int))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? (int)l : 0;
            }
            if (key.PropertyType == typeof(int?))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? (int)l : null;
            }
            if (key.PropertyType == typeof(uint))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? unchecked((uint)l) : 0U;
            }
            if (key.PropertyType == typeof(uint?))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? unchecked((uint)l) : null;
            }
            if (key.PropertyType == typeof(long))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) ?? 0L;
            }
            if (key.PropertyType == typeof(long?))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding);
            }
            if (key.PropertyType == typeof(ulong))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? unchecked((ulong)l) : 0UL;
            }
            if (key.PropertyType == typeof(ulong?))
            {
                return static (source, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.ReadNumericInteger(source, encoding) is long l ? unchecked((ulong)l) : null;
            }
        }
        if (key.PropertyType == typeof(float))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) is double d ? (float)d : 0f;
        }
        if (key.PropertyType == typeof(float?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) is double d ? (float)d : null;
        }
        if (key.PropertyType == typeof(double))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator) ?? 0D;
        }
        if (key.PropertyType == typeof(double?))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadNumericDouble(source, encoding, decimalSeparator);
        }
        throw new ArgumentException("Numeric fields must be of a type convertible to double", nameof(key));
    }

    private static DeserializeField GetOleDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoOle(source, encoding, memo);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoOle(source, encoding, memo)?.ToCharArray();
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoOle(source, encoding, memo).AsMemory();
        }
        throw new ArgumentException("Ole fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetPictureDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoPicture(source, encoding, memo);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoPicture(source, encoding, memo)?.ToCharArray();
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadMemoPicture(source, encoding, memo).AsMemory();
        }
        throw new ArgumentException("Picture fields must be of a type convertible to string", nameof(key));
    }

    private static DeserializeField GetTimestampDeserializer(DeserializerKey key)
        => GetDateTimeDeserializer(key);

    private static DeserializeField GetVariantDeserializer(DeserializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadVariant(source, encoding);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadVariant(source, encoding);
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (source, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.ReadVariant(source, encoding);
        }
        throw new ArgumentException("Variant fields must be of a type convertible to string", nameof(key));
    }
}
