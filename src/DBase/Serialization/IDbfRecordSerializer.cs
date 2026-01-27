namespace DBase.Serialization;

internal interface IDbfRecordSerializer<T>
{
    void Serialize(Span<byte> target, T record, DbfSerializationContext context);
    T Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context);
}
