using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DBase;

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly record struct DbfRecord : IEquatable<DbfRecord>, IReadOnlyList<DbfField>
{
    internal DbfRecordStatus Status { get; init; }
    internal ImmutableArray<DbfField> Fields { get; init; }

    public bool IsDeleted => Status is DbfRecordStatus.Deleted;

    public int Count => Fields.Length;

    public DbfField this[int index] => Fields[index];

    public DbfRecord(DbfRecordStatus status, params ImmutableArray<DbfField> fields)
    {
        Status = status;
        Fields = fields;
    }

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}";

    public IEnumerator<DbfField> GetEnumerator() => Fields.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(DbfRecord other) =>
        Status == other.Status && Fields.SequenceEqual(other.Fields);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Status);
        foreach (var field in Fields)
            hash.Add(field);
        return hash.ToHashCode();
    }

    public static implicit operator ReadOnlySpan<DbfField>(DbfRecord record) => record.Fields.AsSpan();
}
