using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DBase.Serialization;

internal delegate void SerializeRecord<in T>(Span<byte> target, T record, DbfRecordStatus status, ReadOnlySpan<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Memo? memo);

internal static partial class DbfRecordSerializer
{
    private static readonly ConcurrentDictionary<Type, Delegate> s_serializers = [];

    public static SerializeRecord<T> GetSerializer<T>(ReadOnlySpan<DbfFieldDescriptor> descriptors) =>
        (SerializeRecord<T>)s_serializers.GetOrAdd(typeof(T), CreateSerializer<T>, descriptors);

    private static SerializeRecord<T> CreateSerializer<T>(Type type, ReadOnlySpan<DbfFieldDescriptor> descriptors)
    {
        var properties = type.GetProperties();

        if (properties.Length != descriptors.Length)
        {
            throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
        }

        var serializers = new SerializeField[descriptors.Length];
        for (var i = 0; i < descriptors.Length; ++i)
        {
            serializers[i] = DbfFieldSerializer.GetSerializer(properties[i].PropertyType, in descriptors[i]);
        }

        var getters = new Func<T, object?>[properties.Length];
        for (var i = 0; i < properties.Length; ++i)
        {
            getters[i] = GetGetter<T>(properties[i]);
        }

        return (target, record, status, descriptors, encoding, decimalSeparator, memo) =>
        {
            target[0] = (byte)status;
            for (var i = 0; i < descriptors.Length; ++i)
            {
                ref readonly var descriptor = ref descriptors[i];
                ref readonly var serializer = ref serializers[i];
                ref readonly var getter = ref getters[i];
                serializer(target.Slice(descriptor.Offset, descriptor.Length), getter(record), descriptor, encoding, decimalSeparator, memo);
            }
        };
    }

    private static Func<T, object?> GetGetter<T>(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var propertyAccess = Expression.Property(instance, property);
        var castPropertyValue = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<T, object?>>(castPropertyValue, instance).Compile();
    }
}
