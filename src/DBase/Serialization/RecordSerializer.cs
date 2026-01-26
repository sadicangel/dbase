using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;

namespace DBase.Serialization;

internal static class RecordSerializer
{
    private static readonly ConcurrentDictionary<Type, object> s_serializers = [];

    private static RecordSerializer<T> GetSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors) =>
        (RecordSerializer<T>)s_serializers.GetOrAdd(typeof(T), static (_, descriptors) => new RecordSerializer<T>(descriptors), descriptors);

    public static void Serialize<T>(Span<byte> target, T record, DbfRecordStatus status, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Memo? memo) =>
        GetSerializer<T>(descriptors).Serialize(target, record, status, encoding, decimalSeparator, memo);

    public static T Deserialize<T>(ReadOnlySpan<byte> source, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Memo? memo) =>
        GetSerializer<T>(descriptors).Deserialize(source, encoding, decimalSeparator, memo);
}

internal sealed class RecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = new();
    private readonly DbfRecordCodec<T> _dbfRecordCodec = new(descriptors);

    public void Serialize(Span<byte> target, T record, DbfRecordStatus status, Encoding encoding, char decimalSeparator, Memo? memo) =>
        _dbfRecordCodec.Write(target, status, _typeProjection.Values(record), encoding, decimalSeparator, memo);

    public T Deserialize(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator, Memo? memo) =>
        _typeProjection.Create(_dbfRecordCodec.Read(source, encoding, decimalSeparator, memo));
}
