using System.Collections.Immutable;

namespace DBase.Serialization;

internal readonly record struct DbfRecordFormatter<T>
{
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly ImmutableArray<DbfFieldFormatter> _formatters;

    public DbfRecordFormatter(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var properties = typeof(T).GetProperties();

        if (properties.Length != descriptors.Length)
        {
            // TODO: Improve exception message to include type and missing property names.
            throw new InvalidOperationException($"The number of properties does not match the number of field descriptors. Expected: {descriptors.Length}, Actual: {properties.Length}");
        }

        var formatters = ImmutableArray.CreateBuilder<DbfFieldFormatter>(descriptors.Length);
        foreach (var (property, descriptor) in properties.Zip(descriptors))
        {
            formatters.Add(DbfFieldFormatter.Create(property.PropertyType, descriptor));
        }

        _descriptors = descriptors;
        _formatters = formatters.MoveToImmutable();
    }

    public object?[] Read(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        _ = (DbfRecordStatus)source[0];

        var values = new object?[_descriptors.Length];
        var i = 0;
        foreach (var (descriptor, reader) in _descriptors.Zip(_formatters))
        {
            values[i++] = reader.Read(source.Slice(descriptor.Offset, descriptor.Length), context);
        }

        return values;
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, DbfSerializationContext context)
    {
        target[0] = (byte)status;
        foreach (var (descriptor, writer, value) in _descriptors.Zip(_formatters, values))
        {
            writer.Write(target.Slice(descriptor.Offset, descriptor.Length), value, context);
        }
    }
}
