using System.Text;

namespace DBase;

public readonly record struct DbtRecord : IEquatable<DbtRecord>
{
    private readonly byte[] _bytes;

    public ReadOnlySpan<byte> Data => _bytes;

    public DbtRecord(ReadOnlySpan<byte> data) => _bytes = data.ToArray();

    public DbtRecord(ReadOnlySpan<char> data)
    {
        _bytes = new byte[Encoding.ASCII.GetByteCount(data)];
        Encoding.ASCII.GetBytes(data, _bytes);
    }

    public override string ToString() => Encoding.ASCII.GetString(_bytes);

    public bool Equals(DbtRecord other) => _bytes.AsSpan().SequenceEqual(other._bytes.AsSpan());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _bytes.AsSpan())
            hash.Add(item);
        return hash.ToHashCode();
    }
}
