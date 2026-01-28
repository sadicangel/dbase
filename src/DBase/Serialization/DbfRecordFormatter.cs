using System.Collections.Immutable;

namespace DBase.Serialization;

internal readonly struct DbfRecordFormatter<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly ImmutableArray<DbfFieldFormatter> _formatters = CreateFormatters(descriptors);

    public static ImmutableArray<DbfFieldFormatter> CreateFormatters(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var propertyTypes = descriptors.GetPropertyTypes<T>();
        var formatters = ImmutableArray.CreateBuilder<DbfFieldFormatter>(descriptors.Length);
        foreach (var (propertyType, descriptor) in propertyTypes.Zip(descriptors))
        {
            formatters.Add(DbfFieldFormatter.Create(propertyType, descriptor));
        }

        return formatters.MoveToImmutable();
    }

    public object?[] Read(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        _ = (DbfRecordStatus)source[0];

        var values = new object?[descriptors.Length];
        var i = 0;
        foreach (var (descriptor, reader) in descriptors.Zip(_formatters))
        {
            values[i++] = reader.Read(source.Slice(descriptor.Offset, descriptor.Length), context);
        }

        return values;
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, DbfSerializationContext context)
    {
        target[0] = (byte)status;
        foreach (var (descriptor, writer, value) in descriptors.Zip(_formatters, values))
        {
            writer.Write(target.Slice(descriptor.Offset, descriptor.Length), value, context);
        }
    }
}
