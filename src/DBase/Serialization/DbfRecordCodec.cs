using System.Collections.Immutable;
using System.Text;

namespace DBase.Serialization;

internal readonly record struct DbfRecordCodec<T>
{
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly ImmutableArray<DeserializeField> _deserializers;
    private readonly ImmutableArray<SerializeField> _serializers;

    public DbfRecordCodec(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var properties = typeof(T).GetProperties();

        if (properties.Length != descriptors.Length)
        {
            // TODO: Improve exception message to include type and missing property names.
            throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
        }

        var deserializers = ImmutableArray.CreateBuilder<DeserializeField>(descriptors.Length);
        var serializers = ImmutableArray.CreateBuilder<SerializeField>(descriptors.Length);
        foreach (var (property, descriptor) in properties.Zip(descriptors))
        {
            deserializers.Add(DbfFieldSerializer.GetDeserializer(property.PropertyType, in descriptor));
            serializers.Add(DbfFieldSerializer.GetSerializer(property.PropertyType, in descriptor));
        }

        _descriptors = descriptors;
        _deserializers = deserializers.MoveToImmutable();
        _serializers = serializers.ToImmutable();
    }

    public object?[] Read(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator, Memo? memo)
    {
        _ = (DbfRecordStatus)source[0];

        var values = new object?[_descriptors.Length];
        var i = 0;
        foreach (var (descriptor, deserializer) in _descriptors.Zip(_deserializers))
        {
            values[i++] = deserializer(source.Slice(descriptor.Offset, descriptor.Length), encoding, decimalSeparator, memo);
        }

        return values;
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, Encoding encoding, char decimalSeparator, Memo? memo)
    {
        target[0] = (byte)status;
        foreach (var (descriptor, serializer, value) in _descriptors.Zip(_serializers, values))
        {
            serializer(target.Slice(descriptor.Offset, descriptor.Length), value, encoding, decimalSeparator, memo);
        }
    }
}
