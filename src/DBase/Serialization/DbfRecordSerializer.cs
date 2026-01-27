using System.Collections.Immutable;

namespace DBase.Serialization;

internal sealed class DbfRecordSerializer(ImmutableArray<DbfFieldDescriptor> descriptors)
    : IDbfRecordSerializer<DbfRecord>
{
    private readonly DbfRecordFormatter _recordFormatter = new(descriptors);

    public void Serialize(Span<byte> target, DbfRecord record, DbfSerializationContext context) =>
        _recordFormatter.Write(target, record, context);

    public DbfRecord Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
        _recordFormatter.Read(source, context);
}
