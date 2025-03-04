using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DBase.Serialization;

public delegate T DeserializeRecord<T>(ReadOnlySpan<byte> source, ReadOnlySpan<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Memo? memo);

internal static partial class DbfRecordSerializer
{
    private static readonly ConcurrentDictionary<Type, Delegate> s_deserializers = [];

    public static DeserializeRecord<T> GetDeserializer<T>(ReadOnlySpan<DbfFieldDescriptor> descriptors) =>
        (DeserializeRecord<T>)s_deserializers.GetOrAdd(typeof(T), CreateDeserializer<T>, descriptors);

    private static DeserializeRecord<T> CreateDeserializer<T>(Type type, ReadOnlySpan<DbfFieldDescriptor> descriptors)
    {
        var properties = type.GetProperties();

        if (properties.Length != descriptors.Length)
        {
            throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
        }

        var deserializers = new DeserializeField[descriptors.Length];
        for (var i = 0; i < descriptors.Length; ++i)
        {
            deserializers[i] = DbfFieldSerializer.GetDeserializer(properties[i].PropertyType, in descriptors[i]);
        }

        var constructor = GetConstructor<T>([.. properties.Select(p => p.PropertyType)], out var isParameterless);

        if (isParameterless)
        {
            var setters = new Action<T, object?>[properties.Length];
            for (var i = 0; i < properties.Length; ++i)
            {
                setters[i] = GetSetter<T>(properties[i]);
            }

            return (source, descriptors, encoding, decimalSeparator, memo) =>
            {
                var record = constructor([]);

                _ = (DbfRecordStatus)source[0];
                var offset = 1;
                for (var i = 0; i < descriptors.Length; ++i)
                {
                    ref readonly var descriptor = ref descriptors[i];
                    ref readonly var deserializer = ref deserializers[i];
                    ref readonly var setter = ref setters[i];
                    var value = deserializer(source.Slice(descriptor.Offset, descriptor.Length), descriptor, encoding, decimalSeparator, memo);
                    setter(record, value);
                    offset += descriptor.Length;
                }
                return record;
            };
        }

        return (source, descriptors, encoding, decimalSeparator, memo) =>
        {
            _ = (DbfRecordStatus)source[0];
            var offset = 1;

            var args = new object?[descriptors.Length];
            for (var i = 0; i < descriptors.Length; ++i)
            {
                ref readonly var descriptor = ref descriptors[i];
                ref readonly var deserializer = ref deserializers[i];
                args[i] = deserializer(source.Slice(descriptor.Offset, descriptor.Length), descriptor, encoding, decimalSeparator, memo);
                offset += descriptor.Length;
            }

            return constructor(args);
        };

    }

    private static Action<T, object?> GetSetter<T>(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var propertyAccess = Expression.Property(instance, property);
        var castPropertyValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(propertyAccess, castPropertyValue);
        return Expression.Lambda<Action<T, object?>>(assign, instance, value).Compile();
    }

    private static Func<object?[], T> GetConstructor<T>(Type[] args, out bool isParameterless)
    {
        isParameterless = true;

        var type = typeof(T);

        var constructor = type.GetConstructor(args)
            ?? type.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"Type {type} does not have a parameterless constructor or a constructor with the arguments {string.Join(", ", args.Select(a => a.ToString()))}");

        var parameter = Expression.Parameter(typeof(object[]), "args");

        var arguments = Array.Empty<Expression>();

        if (constructor.GetParameters().Length > 0)
        {
            isParameterless = false;

            arguments = new Expression[args.Length];
            for (var i = 0; i < args.Length; ++i)
            {
                arguments[i] = Expression.Convert(Expression.ArrayIndex(parameter, Expression.Constant(i)), args[i]);
            }
        }

        return Expression.Lambda<Func<object?[], T>>(Expression.New(constructor, arguments), parameter).Compile();
    }
}
