using System.Collections.Immutable;

namespace DBase.Serialization;

internal sealed class DbfRecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = new();
    private readonly DbfRecordFormatter<T> _recordFormatter = new(descriptors);

    public void Serialize(Span<byte> target, T record, in DbfSerializationContext context)
    {
        var status = record is DbfRecord dbfRecord
            ? dbfRecord.Status
            : DbfRecordStatus.Valid;

        _recordFormatter.Write(target, status, _typeProjection.Values(record), in context);
    }

    public T Deserialize(ReadOnlySpan<byte> source, in DbfSerializationContext context)
    {
        var record = _recordFormatter.Read(source, in context);
        return _typeProjection.Create(record.Status, record.Values);
    }
}
