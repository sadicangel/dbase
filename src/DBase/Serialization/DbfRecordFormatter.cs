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
        var i = 0;
        try
        {
            for (; i < _descriptors.Length; i++)
            {
                var descriptor = _descriptors[i];
                values[i] = _formatters[i].Read(source.Slice(descriptor.Offset, descriptor.Length), in context);
            }
        }
        catch (Exception exception) when (DbfSerializationContext.IsFieldError(exception))
        {
            throw context.CreateException(i, _descriptors[i], _propertyTypes[i], exception);
        }

        return new DbfRecordValues(status, values);
    }

    public void Write(Span<byte> target, DbfRecordStatus status, object?[] values, in DbfSerializationContext context)
    {
        target[0] = (byte)status;
        // Preserve the existing Zip behavior when fewer values than descriptors are supplied.
        var count = Math.Min(_descriptors.Length, values.Length);
        var i = 0;
        try
        {
            for (; i < count; i++)
            {
                var descriptor = _descriptors[i];
                _formatters[i].Write(target.Slice(descriptor.Offset, descriptor.Length), values[i], in context);
            }
        }
        catch (Exception exception) when (DbfSerializationContext.IsFieldError(exception))
        {
            throw context.CreateException(i, _descriptors[i], _propertyTypes[i], exception);
        }
    }
}
