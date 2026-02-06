using System.Buffers.Binary;

namespace DBase.Serialization.Fields;

internal static class DbfFieldDateTimeFormatter
{
    public static DateTime? ReadRaw(ReadOnlySpan<byte> source)
    {
        var julian = BinaryPrimitives.ReadInt32LittleEndian(source);
        if (julian is 0) return null;
        var milliseconds = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4, 4));
        return DateTime.FromOADate(julian - 2415019.0).AddMilliseconds(milliseconds);
    }

    public static void WriteRaw(Span<byte> target, DateTime? value)
    {
        if (value is null)
        {
            target.Clear();
            return;
        }

        var dateTime = value.Value;
        var julian = (int)(dateTime.Date.ToOADate() + 2415019.0);
        BinaryPrimitives.WriteInt32LittleEndian(target, julian);
        var milliseconds = (int)dateTime.TimeOfDay.TotalMilliseconds;
        BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4, 4), milliseconds);
    }

    public static DbfFieldFormatter Create(Type propertyType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                (DbfField)ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DbfField)value!).GetValue<DateTime?>());
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _) =>
                ReadRaw(source);

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, (DateTime?)value);
        }

        if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
        {
            return new DbfFieldFormatter(Read, Write);

            static object? Read(ReadOnlySpan<byte> source, DbfSerializationContext _)
            {
                var dt = ReadRaw(source);
                return dt is null ? null : new DateTimeOffset(dt.Value);
            }

            static void Write(Span<byte> target, object? value, DbfSerializationContext _) =>
                WriteRaw(target, ((DateTimeOffset?)value)?.DateTime);
        }

        throw new ArgumentException("DateTime fields must be of a type convertible to DateTime", nameof(propertyType));
    }
}
