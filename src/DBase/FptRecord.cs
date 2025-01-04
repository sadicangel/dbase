using System.Text;

namespace DBase;

public readonly record struct FptRecord : IEquatable<FptRecord>
{
    private readonly byte[] _bytes;

    public FptRecordType Type { get; init; }

    public ReadOnlySpan<byte> Data => _bytes;

    internal FptRecord(FptRecordType type, byte[] data)
    {
        Type = type;
        _bytes = data;
    }

    public FptRecord(FptRecordType type, ReadOnlySpan<byte> data)
    {
        Type = type;
        _bytes = data.ToArray();
    }

    public FptRecord(FptRecordType type, ReadOnlySpan<char> data)
    {
        Type = type;
        _bytes = new byte[Encoding.ASCII.GetByteCount(data)];
        Encoding.ASCII.GetBytes(data, _bytes);
    }

    public override string ToString() => Encoding.ASCII.GetString(_bytes);

    public bool Equals(FptRecord other) =>
        Type == other.Type && _bytes.AsSpan().SequenceEqual(other._bytes.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        foreach (var item in _bytes.AsSpan())
            hash.Add(item);
        return hash.ToHashCode();
    }
}
