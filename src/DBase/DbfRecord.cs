using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DBase;

/// <summary>
/// Represents a record of a dBASE file.
/// </summary>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly record struct DbfRecord : IReadOnlyList<DbfField>
{
    internal DbfRecordStatus Status { get; init; }
    internal ImmutableArray<DbfField> Fields { get; init; }

    /// <summary>
    /// Gets a value indicating whether the record is deleted.
    /// </summary>
    public bool IsDeleted => Status is DbfRecordStatus.Deleted;

    /// <summary>
    /// Gets the number of fields in the record.
    /// </summary>
    public int Count => Fields.Length;

    /// <summary>
    /// Gets the field at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the field to get.</param>
    /// <returns>The <see cref="DbfField"/> at the specified index.</returns>
    public DbfField this[int index] => Fields[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord"/> record.
    /// </summary>
    /// <param name="status">The record status.</param>
    /// <param name="fields">The fields of the record.</param>
    public DbfRecord(DbfRecordStatus status, params ImmutableArray<DbfField> fields)
    {
        Status = status;
        Fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord"/> record.
    /// </summary>
    /// <param name="fields">The fields of the record.</param>
    public DbfRecord(params ImmutableArray<DbfField> fields)
    {
        Status = DbfRecordStatus.Valid;
        Fields = fields;
    }

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}";

    /// <summary>
    /// Returns an enumerator that iterates through the fields.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the fields.</returns>
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

    /// <inheritdoc />
    public static implicit operator ReadOnlySpan<DbfField>(DbfRecord record) => record.Fields.AsSpan();
}
