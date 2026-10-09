using System.Collections.Immutable;

namespace DBase.Serialization;

internal static class SerializerExtensions
{
    private static readonly Dictionary<ImmutableArray<DbfFieldDescriptor>, Dictionary<Type, object>> s_cache = [];

    extension(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        public DbfRecordSerializer<T> GetSerializer<T>()
        {
            lock (s_cache)
            {
                if (!s_cache.TryGetValue(descriptors, out var typeCache))
                {
                    s_cache[descriptors] = typeCache = new Dictionary<Type, object>();
                }

                if (!typeCache.TryGetValue(typeof(T), out var serializer))
                {
                    typeCache[typeof(T)] = serializer = new DbfRecordSerializer<T>(descriptors);
                }

                return (DbfRecordSerializer<T>)serializer;
            }
        }

        public ImmutableArray<Type> GetPropertyTypes<T>()
        {
            if (typeof(T) == typeof(DbfRecord))
            {
                return ImmutableArray.CreateRange(Enumerable.Repeat(typeof(DbfField), descriptors.Length));
            }

            var properties = DbfTypeProperties.GetMappedProperties(typeof(T));
            if (properties.Length != descriptors.Length)
            {
                throw new InvalidOperationException(
                    $"The number of mapped properties on target record type '{typeof(T).FullName}' does not match the number of field descriptors. " +
                    $"Field descriptors ({descriptors.Length}): [{string.Join(", ", descriptors.Select(static descriptor => descriptor.Name.ToString()))}]. " +
                    $"Mapped properties ({properties.Length}): [{string.Join(", ", properties.Select(static property => property.Name))}].");
            }

            return ImmutableArray.CreateRange(properties.Select(static property => property.PropertyType));
        }
    }
}
