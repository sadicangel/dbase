using System.Collections.Immutable;
using System.Text;

namespace DBase.Serialization;

internal readonly record struct DbfRecordCodec<T>
{
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly ImmutableArray<DbfFieldCodec> _fieldCodecs;

    public DbfRecordCodec(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var properties = typeof(T).GetProperties();

        if (properties.Length != descriptors.Length)
        {
            // TODO: Improve exception message to include type and missing property names.
            throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
        }

        var fieldCodecs = ImmutableArray.CreateBuilder<DbfFieldCodec>(descriptors.Length);
        foreach (var (property, descriptor) in properties.Zip(descriptors))
        {
            fieldCodecs.Add(new DbfFieldCodec(property, descriptor));
        }

        _descriptors = descriptors;
        _fieldCodecs = fieldCodecs.MoveToImmutable();
    }

    public object?[] Read(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator, Memo? memo)
    {
        _ = (DbfRecordStatus)source[0];

        var values = new object?[_descriptors.Length];
        var i = 0;
        foreach (var (descriptor, reader) in _descriptors.Zip(_fieldCodecs))
        {
            values[i++] = reader.Read(source.Slice(descriptor.Offset, descriptor.Length), encoding, decimalSeparator, memo);
        }

        return values;
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, Encoding encoding, char decimalSeparator, Memo? memo)
    {
        target[0] = (byte)status;
        foreach (var (descriptor, writer, value) in _descriptors.Zip(_fieldCodecs, values))
        {
            writer.Write(target.Slice(descriptor.Offset, descriptor.Length), value, encoding, decimalSeparator, memo);
        }
    }
}
