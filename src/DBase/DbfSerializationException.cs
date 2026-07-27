namespace DBase;

/// <summary>
/// Represents an error that occurred while serializing or deserializing a DBF field.
/// </summary>
/// <remarks>
/// The exception preserves the original formatter failure in <see cref="Exception.InnerException"/> and
/// adds record, field, schema, and target CLR type details for diagnostics.
/// </remarks>
public sealed class DbfSerializationException : Exception
{
    internal DbfSerializationException(
        DbfSerializationOperation operation,
        int recordIndex,
        int fieldIndex,
        DbfFieldDescriptor descriptor,
        Type targetClrType,
        Type recordType,
        DbfVersion version,
        DbfLanguage language,
        Exception innerException)
        : base(CreateMessage(operation, recordIndex, fieldIndex, descriptor, targetClrType, recordType, version, language, innerException), innerException)
    {
        Operation = operation;
        RecordIndex = recordIndex;
        FieldIndex = fieldIndex;
        Descriptor = descriptor;
        TargetClrType = targetClrType;
        RecordType = recordType;
        Version = version;
        Language = language;
    }

    /// <summary>
    /// Gets the serialization operation that failed.
    /// </summary>
    public DbfSerializationOperation Operation { get; }

    /// <summary>
    /// Gets the zero-based physical record index that was being processed.
    /// </summary>
    public int RecordIndex { get; }

    /// <summary>
    /// Gets the zero-based field index that was being processed.
    /// </summary>
    public int FieldIndex { get; }

    /// <summary>
    /// Gets the DBF field descriptor that was being processed.
    /// </summary>
    public DbfFieldDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the DBF field name that was being processed.
    /// </summary>
    public DbfFieldName FieldName => Descriptor.Name;

    /// <summary>
    /// Gets the DBF field type that was being processed.
    /// </summary>
    public DbfFieldType FieldType => Descriptor.Type;

    /// <summary>
    /// Gets the CLR field value type used by the formatter.
    /// </summary>
    public Type TargetClrType { get; }

    /// <summary>
    /// Gets the CLR record type being serialized or deserialized.
    /// </summary>
    public Type RecordType { get; }

    /// <summary>
    /// Gets the DBF table version being processed.
    /// </summary>
    public DbfVersion Version { get; }

    /// <summary>
    /// Gets the DBF language-driver marker being processed.
    /// </summary>
    public DbfLanguage Language { get; }

    private static string CreateMessage(
        DbfSerializationOperation operation,
        int recordIndex,
        int fieldIndex,
        DbfFieldDescriptor descriptor,
        Type targetClrType,
        Type recordType,
        DbfVersion version,
        DbfLanguage language,
        Exception innerException)
    {
        var action = operation is DbfSerializationOperation.Read ? "read" : "write";
        return $"Failed to {action} DBF field '{descriptor.Name}' at record {recordIndex}, field {fieldIndex} " +
               $"({descriptor.Type}, length {descriptor.Length}) as CLR type '{targetClrType.FullName}' " +
               $"for record type '{recordType.FullName}' in {version} table with language {language}: {innerException.Message}";
    }
}
