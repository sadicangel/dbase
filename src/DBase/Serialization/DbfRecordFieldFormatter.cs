using System.Collections.Immutable;
using System.Runtime.InteropServices;
using DBase.Serialization.Fields;

namespace DBase.Serialization;

/// <summary>
/// Serializes untyped DBF records without boxing field values.
/// </summary>
internal readonly struct DbfRecordFieldFormatter(ImmutableArray<DbfFieldDescriptor> descriptors)
{
    /// <summary>
    /// Deserializes a DBF record from the specified byte sequence.
    /// </summary>
    /// <param name="source">The bytes containing the record status and field data.</param>
    /// <param name="context">The serialization context.</param>
    /// <returns>The deserialized record.</returns>
    public DbfRecord Read(ReadOnlySpan<byte> source, DbfSerializationContext context)
    {
        var fields = new DbfField[descriptors.Length];
        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = descriptors[i];
            fields[i] = ReadField(source.Slice(descriptor.Offset, descriptor.Length), descriptor, context);
        }

        // The freshly allocated array is owned exclusively by the returned immutable record.
        return new DbfRecord((DbfRecordStatus)source[0], ImmutableCollectionsMarshal.AsImmutableArray(fields));
    }

    /// <summary>
    /// Serializes the specified DBF record to the destination span.
    /// </summary>
    /// <param name="target">The destination for the record status and field data.</param>
    /// <param name="record">The record to serialize.</param>
    /// <param name="context">The serialization context.</param>
    public void Write(Span<byte> target, DbfRecord record, DbfSerializationContext context)
    {
        target[0] = (byte)record.Status;
        // Preserve the existing formatter's Zip behavior for records with fewer fields than descriptors.
        var count = Math.Min(descriptors.Length, record.Count);
        for (var i = 0; i < count; i++)
        {
            var descriptor = descriptors[i];
            WriteField(target.Slice(descriptor.Offset, descriptor.Length), record[i], descriptor, context);
        }
    }

    private static DbfField ReadField(ReadOnlySpan<byte> source, DbfFieldDescriptor descriptor, DbfSerializationContext context) => descriptor.Type switch
    {
        DbfFieldType.AutoIncrement => DbfFieldAutoIncrementFormatter.ReadRaw(source),
        DbfFieldType.Binary when descriptor.Length == 8 => DbfFieldDoubleFormatter.ReadRaw(source),
        DbfFieldType.Binary or DbfFieldType.Blob or DbfFieldType.Ole =>
            DbfFieldMemoFormatter.ReadMemo(source, MemoRecordType.Object, context.Encoding, context.Memo),
        DbfFieldType.Character => DbfFieldCharacterFormatter.ReadRaw(source, context.Encoding),
        DbfFieldType.Currency => DbfFieldCurrencyFormatter.ReadRaw(source),
        DbfFieldType.Date => DbfFieldDateFormatter.ReadRaw(source, context.Encoding),
        DbfFieldType.DateTime or DbfFieldType.Timestamp => DbfFieldDateTimeFormatter.ReadRaw(source),
        DbfFieldType.Double => DbfFieldDoubleFormatter.ReadRaw(source),
        DbfFieldType.Float or DbfFieldType.Numeric when descriptor.Decimal == 0 =>
            DbfFieldNumericFormatter.ReadRaw(source, context.Encoding),
        DbfFieldType.Float or DbfFieldType.Numeric =>
            DbfFieldNumericFormatter.ReadRaw(source, context.Encoding, context.DecimalSeparator),
        DbfFieldType.Int32 => DbfFieldInt32Formatter.ReadRaw(source),
        DbfFieldType.Logical => DbfFieldLogicalFormatter.ReadRaw(source, context.Encoding),
        DbfFieldType.Memo => DbfFieldMemoFormatter.ReadMemo(source, MemoRecordType.Memo, context.Encoding, context.Memo),
        DbfFieldType.NullFlags => DbfFieldNullFlagsFormatter.ReadRaw(source),
        DbfFieldType.Picture => DbfFieldMemoFormatter.ReadMemo(source, MemoRecordType.Picture, context.Encoding, context.Memo),
        DbfFieldType.Variant => DbfFieldVariantFormatter.ReadRaw(source, context.Encoding),
        _ => throw new NotSupportedException($"Field type '{descriptor.Type}' is not supported.")
    };

    private static void WriteField(Span<byte> target, DbfField value, DbfFieldDescriptor descriptor, DbfSerializationContext context)
    {
        switch (descriptor.Type)
        {
            case DbfFieldType.AutoIncrement:
                DbfFieldAutoIncrementFormatter.WriteRaw(target, DbfFieldAutoIncrementFormatter.ConvertToInt32(value));
                break;
            case DbfFieldType.Binary when descriptor.Length == 8:
            case DbfFieldType.Double:
                DbfFieldDoubleFormatter.WriteRaw(target, value.GetValue<double>());
                break;
            case DbfFieldType.Binary:
            case DbfFieldType.Blob:
            case DbfFieldType.Ole:
                DbfFieldMemoFormatter.WriteMemo(target, MemoRecordType.Object, value.GetValue<string>(), context.Encoding, context.Memo);
                break;
            case DbfFieldType.Character:
                DbfFieldCharacterFormatter.WriteRaw(target, value.GetValue<string>(), context.Encoding);
                break;
            case DbfFieldType.Currency:
                DbfFieldCurrencyFormatter.WriteRaw(target, value.GetValue<decimal>());
                break;
            case DbfFieldType.Date:
                DbfFieldDateFormatter.WriteRaw(target, value.GetValue<DateTime?>(), context.Encoding);
                break;
            case DbfFieldType.DateTime:
            case DbfFieldType.Timestamp:
                DbfFieldDateTimeFormatter.WriteRaw(target, value.GetValue<DateTime?>());
                break;
            case DbfFieldType.Float when descriptor.Decimal == 0:
            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                DbfFieldNumericFormatter.WriteRaw(target, value.GetValue<long?>(), context.Encoding);
                break;
            case DbfFieldType.Float:
            case DbfFieldType.Numeric:
                DbfFieldNumericFormatter.WriteRaw(target, value.GetValue<double?>(), descriptor.Decimal, context.Encoding, context.DecimalSeparator);
                break;
            case DbfFieldType.Int32:
                DbfFieldInt32Formatter.WriteRaw(target, value.GetValue<int>());
                break;
            case DbfFieldType.Logical:
                DbfFieldLogicalFormatter.WriteRaw(target, value.GetValue<bool?>());
                break;
            case DbfFieldType.Memo:
                DbfFieldMemoFormatter.WriteMemo(target, MemoRecordType.Memo, value.GetValue<string>(), context.Encoding, context.Memo);
                break;
            case DbfFieldType.NullFlags:
                DbfFieldNullFlagsFormatter.WriteRaw(target, value.GetValue<string>());
                break;
            case DbfFieldType.Picture:
                DbfFieldMemoFormatter.WriteMemo(target, MemoRecordType.Picture, value.GetValue<string>(), context.Encoding, context.Memo);
                break;
            case DbfFieldType.Variant:
                DbfFieldVariantFormatter.WriteRaw(target, value.GetValue<string>(), context.Encoding);
                break;
            default:
                throw new NotSupportedException($"Field type '{descriptor.Type}' is not supported.");
        }
    }
}
