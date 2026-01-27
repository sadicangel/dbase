using System.Collections.Immutable;

namespace DBase.Serialization;

internal static class SerializerExtensions
{
    private static readonly Dictionary<ImmutableArray<DbfFieldDescriptor>, Dictionary<Type, object>> s_cache = [];

    extension(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        public IDbfRecordSerializer<T> GetSerializer<T>()
        {
            lock (s_cache)
            {
                if (!s_cache.TryGetValue(descriptors, out var typeCache))
                {
                    s_cache[descriptors] = typeCache = new Dictionary<Type, object>();
                }

                if (!typeCache.TryGetValue(typeof(T), out var serializer))
                {
                    typeCache[typeof(T)] = serializer = typeof(T) == typeof(DbfRecord)
                        ? new DbfRecordSerializer(descriptors)
                        : new DbfRecordSerializer<T>(descriptors);
                }

                return (IDbfRecordSerializer<T>)serializer;
            }
        }
    }
}
