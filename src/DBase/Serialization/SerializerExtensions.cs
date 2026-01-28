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

        public IEnumerable<Type> GetPropertyTypes<T>()
        {
            if (typeof(T) == typeof(DbfRecord))
            {
                return Enumerable.Repeat(typeof(DbfField), descriptors.Length);
            }

            var properties = typeof(T).GetProperties();
            if (properties.Length != descriptors.Length)
            {
                // TODO: Improve exception message to include type and missing property names.
                throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
            }

            return properties.Select(x => x.PropertyType);
        }
    }
}
