using System.Collections.Immutable;

namespace DBase.Serialization;

internal sealed class DbfRecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = new();
    private readonly DbfRecordFormatter<T> _recordFormatter = new(descriptors);

    public void Serialize(Span<byte> target, T record, DbfSerializationContext context) =>
        _recordFormatter.Write(target, DbfRecordStatus.Valid, _typeProjection.Values(record), context);

    public T Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
        _typeProjection.Create(_recordFormatter.Read(source, context));
}
