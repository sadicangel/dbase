using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DBase;

/// <summary>
/// Represents a single record (row) in a DBF table.
/// </summary>
/// <remarks>
/// A DBF record starts with a one-byte delete flag followed by the field data, in the order defined by the
/// table header. Each initialized record retains its immutable field descriptors. A default-initialized
/// record has no schema and cannot be written.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly record struct DbfRecord : IReadOnlyList<DbfField>
{
    internal DbfRecordStatus Status { get; }
    internal ImmutableArray<DbfField> Fields { get; }

    /// <summary>
    /// Gets the immutable field descriptors in the same order as the field values.
    /// </summary>
    /// <remarks>
    /// Records read from a table share the table's descriptor array and retain it after the table is disposed.
    /// </remarks>
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }

    /// <summary>
    /// Gets a value indicating whether the record is marked as deleted in the DBF stream.
    /// </summary>
    /// <remarks>
    /// Untyped deserialization and writing preserve the record's status marker.
    /// </remarks>
    public bool IsDeleted => Status is DbfRecordStatus.Deleted;

    /// <summary>
    /// Gets the number of fields in the record (as defined by the header).
    /// </summary>
    public int Count => Fields.Length;

    /// <summary>
    /// Gets the field at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the field to get.</param>
    /// <returns>The <see cref="DbfField"/> at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside record bounds.</exception>
    public DbfField this[int index] => Fields[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord"/> record.
    /// </summary>
    /// <param name="status">The persisted record status byte.</param>
    /// <param name="descriptors">The schema describing the field values.</param>
    /// <param name="fields">Field values in descriptor order.</param>
    /// <exception cref="ArgumentException">
    /// Either array is uninitialized, or the number of fields differs from the number of descriptors.
    /// </exception>
    public DbfRecord(DbfRecordStatus status, ImmutableArray<DbfFieldDescriptor> descriptors, ImmutableArray<DbfField> fields)
    {
        if (descriptors.IsDefault)
            throw new ArgumentException("Field descriptors must be initialized.", nameof(descriptors));
        if (fields.IsDefault)
            throw new ArgumentException("Field values must be initialized.", nameof(fields));
        if (fields.Length != descriptors.Length)
            throw new ArgumentException("Supply exactly one field value per descriptor.", nameof(fields));

        Status = status;
        Descriptors = descriptors;
        Fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord"/> record.
    /// </summary>
    /// <param name="descriptors">The schema describing the field values.</param>
    /// <param name="fields">Field values in descriptor order.</param>
    /// <exception cref="ArgumentException">
    /// Either array is uninitialized, or the number of fields differs from the number of descriptors.
    /// </exception>
    public DbfRecord(ImmutableArray<DbfFieldDescriptor> descriptors, ImmutableArray<DbfField> fields)
        : this(DbfRecordStatus.Valid, descriptors, fields)
    {
    }

    private string GetDebuggerDisplay() => $"[{string.Join(", ", Fields.Select(x => x.ToString()))}]";

    /// <summary>
    /// Returns an enumerator that iterates through the fields.
    /// </summary>
    /// <returns>An enumerator that iterates fields in descriptor order.</returns>
    public IEnumerator<DbfField> GetEnumerator() => Fields.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(DbfRecord other) =>
        Status == other.Status && Fields.SequenceEqual(other.Fields);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Status);
        foreach (var field in Fields)
            hash.Add(field);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Implicitly converts a <see cref="DbfRecord"/> to a read-only span of its fields.
    /// </summary>
    /// <param name="record">The record to convert.</param>
    /// <returns>A span view over the field values.</returns>
    public static implicit operator ReadOnlySpan<DbfField>(DbfRecord record) => record.Fields.AsSpan();
}
