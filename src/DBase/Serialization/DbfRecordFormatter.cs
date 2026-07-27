using System.Collections.Immutable;
using DBase.Serialization.Fields;

namespace DBase.Serialization;

internal readonly record struct DbfRecordValues(DbfRecordStatus Status, object?[] Values);

internal readonly struct DbfRecordFormatter<T>
{
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly ImmutableArray<DbfFieldFormatter> _formatters;
    private readonly ImmutableArray<Type> _propertyTypes;

    public DbfRecordFormatter(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        _descriptors = descriptors;
        _propertyTypes = descriptors.GetPropertyTypes<T>();
        _formatters = CreateFormatters(descriptors, _propertyTypes);
    }

    public static ImmutableArray<DbfFieldFormatter> CreateFormatters(ImmutableArray<DbfFieldDescriptor> descriptors, ImmutableArray<Type> propertyTypes)
    {
        var formatters = ImmutableArray.CreateBuilder<DbfFieldFormatter>(descriptors.Length);
        foreach (var (propertyType, descriptor) in propertyTypes.Zip(descriptors))
        {
            formatters.Add(DbfFieldFormatter.Create(propertyType, descriptor));
        }

        return formatters.MoveToImmutable();
    }

    public DbfRecordValues Read(ReadOnlySpan<byte> source, in DbfSerializationContext context)
    {
        var status = (DbfRecordStatus)source[0];

        var values = new object?[_descriptors.Length];
        for (var i = 0; i < _descriptors.Length; ++i)
        {
            var descriptor = _descriptors[i];
            try
            {
                values[i] = _formatters[i].Read(source.Slice(descriptor.Offset, descriptor.Length), in context);
            }
            catch (Exception exception) when (exception is not DbfSerializationException)
            {
                throw CreateException(in context, i, descriptor, exception);
            }
        }

        return new DbfRecordValues(status, values);
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, in DbfSerializationContext context)
    {
        target[0] = (byte)status;
        var i = 0;
        foreach (var (descriptor, writer, value) in _descriptors.Zip(_formatters, values))
        {
            try
            {
                writer.Write(target.Slice(descriptor.Offset, descriptor.Length), value, in context);
            }
            catch (Exception exception) when (exception is not DbfSerializationException)
            {
                throw CreateException(in context, i, descriptor, exception);
            }

            ++i;
        }
    }

    private DbfSerializationException CreateException(
        in DbfSerializationContext context,
        int fieldIndex,
        DbfFieldDescriptor descriptor,
        Exception exception) =>
        new(
            context.Operation,
            context.RecordIndex,
            fieldIndex,
            descriptor,
            _propertyTypes[fieldIndex],
            context.RecordType,
            context.Version,
            context.Language,
            exception);
}
