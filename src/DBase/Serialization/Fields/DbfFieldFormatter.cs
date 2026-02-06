namespace DBase.Serialization.Fields;

internal delegate object? ReadValue(ReadOnlySpan<byte> source, DbfSerializationContext context);

internal delegate void WriteValue(Span<byte> target, object? value, DbfSerializationContext context);

internal readonly struct DbfFieldFormatter(ReadValue read, WriteValue write)
{
    public object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) => read(source, context);

    public void Write(Span<byte> target, object? value, DbfSerializationContext context) => write(target, value, context);

    public static DbfFieldFormatter Create(Type propertyType, DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => DbfFieldAutoIncrementFormatter.Create(propertyType),
            DbfFieldType.Binary when descriptor.Length == 8 => DbfFieldDoubleFormatter.Create(propertyType),
            DbfFieldType.Binary => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Blob => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Character => DbfFieldCharacterFormatter.Create(propertyType),
            DbfFieldType.Currency => DbfFieldCurrencyFormatter.Create(propertyType),
            DbfFieldType.Date => DbfFieldDateFormatter.Create(propertyType),
            DbfFieldType.DateTime => DbfFieldDateTimeFormatter.Create(propertyType),
            DbfFieldType.Double => DbfFieldDoubleFormatter.Create(propertyType),
            DbfFieldType.Float => DbfFieldNumericFormatter.Create(propertyType, descriptor.Decimal),
            DbfFieldType.Int32 => DbfFieldInt32Formatter.Create(propertyType),
            DbfFieldType.Logical => DbfFieldLogicalFormatter.Create(propertyType),
            DbfFieldType.Memo => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Memo),
            DbfFieldType.NullFlags => DbfFieldNullFlagsFormatter.Create(propertyType),
            DbfFieldType.Numeric => DbfFieldNumericFormatter.Create(propertyType, descriptor.Decimal),
            DbfFieldType.Ole => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Object),
            DbfFieldType.Picture => DbfFieldMemoFormatter.Create(propertyType, MemoRecordType.Picture),
            DbfFieldType.Timestamp => DbfFieldDateTimeFormatter.Create(propertyType),
            DbfFieldType.Variant => DbfFieldVariantFormatter.Create(propertyType),
            _ => throw new NotSupportedException($"Field type '{descriptor.Type}' is not supported.")
        };
    }
}
