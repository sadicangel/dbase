using System.Collections.Immutable;

namespace DBase.Serialization;

internal sealed class DbfRecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = new();
    private readonly DbfRecordFormatter<T> _recordFormatter = new(descriptors);

    public void Serialize(Span<byte> target, T record, DbfSerializationContext context)
    {
        var status = record is DbfRecord dbfRecord
            ? dbfRecord.Status
            : DbfRecordStatus.Valid;

        _recordFormatter.Write(target, status, _typeProjection.Values(record), context);
    }

    public T Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        var record = _recordFormatter.Read(source, context);
        return _typeProjection.Create(record.Status, record.Values);
    }
}
