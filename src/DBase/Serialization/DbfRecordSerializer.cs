using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace DBase.Serialization;

internal static class DbfRecordSerializer
{
    private static readonly ConcurrentDictionary<Type, object> s_serializers = [];

    private static RecordSerializer<T> GetSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors) =>
        (RecordSerializer<T>)s_serializers.GetOrAdd(typeof(T), static (_, descriptors) => new RecordSerializer<T>(descriptors), descriptors);

    public static void Serialize<T>(Span<byte> target, T record, DbfRecordStatus status, ImmutableArray<DbfFieldDescriptor> descriptors, DbfSerializationContext context) =>
        GetSerializer<T>(descriptors).Serialize(target, record, status, context);

    public static T Deserialize<T>(ReadOnlySpan<byte> source, ImmutableArray<DbfFieldDescriptor> descriptors, DbfSerializationContext context) =>
        GetSerializer<T>(descriptors).Deserialize(source, context);
}

internal sealed class RecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = new();
    private readonly DbfRecordFormatter<T> _recordFormatter = new(descriptors);

    public void Serialize(Span<byte> target, T record, DbfRecordStatus status, DbfSerializationContext context) =>
        _recordFormatter.Write(target, status, _typeProjection.Values(record), context);

    public T Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
        _typeProjection.Create(_recordFormatter.Read(source, context));
}
