using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DBase.Serialization;

internal sealed class DbfRecordSerializer<T>(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    private readonly TypeProjection<T> _typeProjection = typeof(T) == typeof(DbfRecord) ? default : new();
    private readonly DbfRecordFormatter<T> _recordFormatter = typeof(T) == typeof(DbfRecord) ? default : new(descriptors);
    private readonly DbfRecordFieldFormatter _fieldFormatter = typeof(T) == typeof(DbfRecord) ? new(descriptors) : default;

    public void Serialize(Span<byte> target, T record, DbfSerializationContext context)
    {
        if (typeof(T) == typeof(DbfRecord))
        {
            // The exact type check permits access to the struct without boxing through object.
            _fieldFormatter.Write(target, Unsafe.As<T, DbfRecord>(ref record), context);
            return;
        }

        _recordFormatter.Write(target, DbfRecordStatus.Valid, _typeProjection.Values(record), context);
    }

    public T Deserialize(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        if (typeof(T) == typeof(DbfRecord))
        {
            var untyped = _fieldFormatter.Read(source, context);
            return Unsafe.As<DbfRecord, T>(ref untyped);
        }

        var record = _recordFormatter.Read(source, context);
        return _typeProjection.Create(record.Status, record.Values);
    }
}
