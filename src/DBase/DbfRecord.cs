using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DBase;

/// <summary>
/// Represents a record of a <see cref="Dbf" />.
/// </summary>
/// <remarks>
/// The record if defined by a <see cref="DbfRecordDescriptor" />.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public sealed class DbfRecord : IEquatable<DbfRecord>, IReadOnlyList<DbfField>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ImmutableArray<DbfField> _fields;

    /// <summary>
    /// The empty record.
    /// </summary>
    public static readonly DbfRecord Empty = new(DbfRecordStatus.Active, []);

    /// <summary>
    /// Gets the status of the record.
    /// </summary>
    public DbfRecordStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is deleted.
    /// </summary>
    public bool IsDeleted => Status == DbfRecordStatus.Deleted;

    /// <summary>
    /// Gets the number of <see cref="DbfField" /> elements.
    /// </summary>
    public int Count => _fields.Length;

    /// <summary>
    /// Gets the <see cref="DbfField"/> at the specified index.
    /// </summary>
    public DbfField this[int index] => _fields[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord" /> class.
    /// </summary>
    /// <param name="fields">The record fields.</param>
    /// <exception cref="ArgumentNullException">fields</exception>
    public DbfRecord(DbfRecordStatus status, params ImmutableArray<DbfField> fields)
    {
        Status = status;
        _fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord" /> class.
    /// </summary>
    /// <param name="other">Another record to copy.</param>
    public DbfRecord(DbfRecord other)
    {
        Status = other.Status;
        _fields = other._fields;
    }

    /// <summary>
    /// Creates a deep copy of this <see cref="DbfRecord" />.
    /// </summary>
    /// <returns>A new <see cref="DbfRecord"/> that is a deep copy of this one.</returns>
    public DbfRecord Clone() => new(this);

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}";

    /// <inheritdoc />
    public IEnumerator<DbfField> GetEnumerator() => _fields.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfRecord? other)
    {
        if (other is null)
            return false;
        return Status == other.Status
            && _fields.SequenceEqual(other._fields);
    }
    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>
    ///   <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => Equals(obj as DbfRecord);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Status);
        foreach (var field in _fields)
            hash.Add(field);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(DbfRecord left, DbfRecord right) => left is null ? right is null : left.Equals(right);
    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(DbfRecord left, DbfRecord right) => !(left == right);
}
