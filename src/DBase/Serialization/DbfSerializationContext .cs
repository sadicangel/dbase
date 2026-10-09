using System.Text;

namespace DBase.Serialization;

internal readonly record struct DbfSerializationContext(
    Encoding Encoding,
    Memo? Memo,
    char DecimalSeparator,
    DbfSerializationOperation Operation,
    int RecordIndex,
    DbfVersion Version,
    DbfLanguage Language,
    Type RecordType)
{
    public DbfSerializationException CreateException(int fieldIndex, DbfFieldDescriptor descriptor, Type targetClrType, Exception innerException) =>
        new(Operation, RecordIndex, fieldIndex, descriptor, targetClrType, RecordType, Version, Language, innerException);

    // Only expected field conversion and I/O failures gain serialization context.
    public static bool IsFieldError(Exception exception) => exception is
        FormatException or OverflowException or InvalidCastException or InvalidOperationException or
        ArgumentException or NotSupportedException or IOException;
}
