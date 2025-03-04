using System.Collections.Concurrent;
using System.Text;
using DBase.Interop;

namespace DBase.Serialization;

public delegate void SerializeField(Span<byte> target, object? value, DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Memo? memo);
public delegate object? DeserializeField(ReadOnlySpan<byte> source, DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Memo? memo);


internal static class DbfFieldSerializer
{
    private readonly record struct SerializerKey(Type PropertyType, DbfFieldType DbfFieldType, byte Length, byte Decimal);

    private static readonly ConcurrentDictionary<SerializerKey, SerializeField> s_serializers = [];

    public static SerializeField GetSerializer(Type propertyType, in DbfFieldDescriptor fieldDescriptor) => s_serializers.GetOrAdd(new(propertyType, fieldDescriptor.Type, fieldDescriptor.Length, fieldDescriptor.Decimal), CreateSerializer);

    private static SerializeField CreateSerializer(SerializerKey key)
    {
        return key.DbfFieldType switch
        {
            DbfFieldType.AutoIncrement => GetAutoIncrementSerializer(key),
            DbfFieldType.Binary => GetBinarySerializer(key),
            DbfFieldType.Blob => throw new NotImplementedException(),
            DbfFieldType.Character => GetCharacterSerializer(key),
            DbfFieldType.Currency => throw new NotImplementedException(),
            DbfFieldType.Date => throw new NotImplementedException(),
            DbfFieldType.DateTime => throw new NotImplementedException(),
            DbfFieldType.Double => throw new NotImplementedException(),
            DbfFieldType.Float => throw new NotImplementedException(),
            DbfFieldType.Int32 => throw new NotImplementedException(),
            DbfFieldType.Logical => throw new NotImplementedException(),
            DbfFieldType.Memo => throw new NotImplementedException(),
            DbfFieldType.NullFlags => throw new NotImplementedException(),
            DbfFieldType.Numeric => throw new NotImplementedException(),
            DbfFieldType.Ole => throw new NotImplementedException(),
            DbfFieldType.Picture => throw new NotImplementedException(),
            DbfFieldType.Timestamp => throw new NotImplementedException(),
            DbfFieldType.Variant => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    private static SerializeField GetAutoIncrementSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(byte))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (byte)value!);
        }

        if (key.PropertyType == typeof(sbyte))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (sbyte)value!);
        }

        if (key.PropertyType == typeof(short))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (short)value!);
        }

        if (key.PropertyType == typeof(ushort))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (ushort)value!);
        }

        if (key.PropertyType == typeof(int))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (int)value!);
        }

        if (key.PropertyType == typeof(uint))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (uint)value!);
        }

        if (key.PropertyType == typeof(long))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, (long)value!);
        }

        if (key.PropertyType == typeof(ulong))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteAutoIncrement(target, unchecked((long)(ulong)value!));
        }

        throw new ArgumentException("AutoIncrement fields must be of a type convertible to Int64", nameof(key));
    }

    private static SerializeField GetBinarySerializer(SerializerKey key)
    {
        if (key.Length is 8)
        {
            if (key.PropertyType == typeof(double))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (double)value!);
            }
            if (key.PropertyType == typeof(float))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (float)value!);
            }
            if (key.PropertyType == typeof(byte))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (byte)value!);
            }
            if (key.PropertyType == typeof(sbyte))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (sbyte)value!);
            }
            if (key.PropertyType == typeof(short))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (short)value!);
            }
            if (key.PropertyType == typeof(ushort))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (ushort)value!);
            }
            if (key.PropertyType == typeof(int))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (int)value!);
            }
            if (key.PropertyType == typeof(uint))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (uint)value!);
            }
            if (key.PropertyType == typeof(long))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (long)value!);
            }
            if (key.PropertyType == typeof(ulong))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteDouble(target, (ulong)value!);
            }

            throw new ArgumentException("Binary fields of length 8 must be of a type convertible to double", nameof(key));
        }
        else
        {
            if (key.PropertyType == typeof(string))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteMemoBinary(target, (string?)value, encoding, memo);
            }
            if (key.PropertyType == typeof(char[]))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteMemoBinary(target, (char[]?)value, encoding, memo);
            }
            if (key.PropertyType == typeof(ReadOnlyMemory<char>))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteMemoBinary(target, ((ReadOnlyMemory<char>)value!).Span, encoding, memo);
            }
            if (key.PropertyType == typeof(Memory<char>))
            {
                return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                    DbfMarshal.WriteMemoBinary(target, ((Memory<char>)value!).Span, encoding, memo);
            }

            throw new ArgumentException("Binary fields must be of a type convertible to string", nameof(key));
        }

    }

    private static SerializeField GetCharacterSerializer(SerializerKey key)
    {
        if (key.PropertyType == typeof(string))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteCharacter(target, (string?)value, encoding);
        }
        if (key.PropertyType == typeof(char[]))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteCharacter(target, (char[]?)value, encoding);
        }
        if (key.PropertyType == typeof(ReadOnlyMemory<char>))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteCharacter(target, ((ReadOnlyMemory<char>)value!).Span, encoding);
        }
        if (key.PropertyType == typeof(Memory<char>))
        {
            return static (target, value, descriptor, encoding, decimalSeparator, memo) =>
                DbfMarshal.WriteCharacter(target, ((Memory<char>)value!).Span, encoding);
        }

        throw new ArgumentException("Character fields must be of a type convertible to string", nameof(key));
    }
}
