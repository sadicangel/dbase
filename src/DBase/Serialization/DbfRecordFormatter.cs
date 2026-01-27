using System.Collections.Immutable;

namespace DBase.Serialization;

internal readonly record struct DbfRecordFormatter
{
    private readonly ImmutableArray<DbfFieldDescriptor> _descriptors;
    private readonly ImmutableArray<DbfFieldFormatter> _formatters;

    public DbfRecordFormatter(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var formatters = ImmutableArray.CreateBuilder<DbfFieldFormatter>(descriptors.Length);
        foreach (var descriptor in descriptors)
        {
            formatters.Add(DbfFieldFormatter.Create(typeof(DbfField), descriptor));
        }

        _descriptors = descriptors;
        _formatters = formatters.MoveToImmutable();
    }

    public DbfRecord Read(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        var status = (DbfRecordStatus)source[0];

        var fields = ImmutableArray.CreateBuilder<DbfField>(_descriptors.Length);
        foreach (var (descriptor, reader) in _descriptors.Zip(_formatters))
        {
            fields.Add((DbfField)reader.Read(source.Slice(descriptor.Offset, descriptor.Length), context)!);
        }

        return new DbfRecord(status, fields.MoveToImmutable());
    }

    public void Write(Span<byte> target, DbfRecord record, DbfSerializationContext context)
    {
        target[0] = (byte)record.Status;
        foreach (var (descriptor, writer, field) in _descriptors.Zip(_formatters, record))
        {
            writer.Write(target.Slice(descriptor.Offset, descriptor.Length), field, context);
        }
    }
}
