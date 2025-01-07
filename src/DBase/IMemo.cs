
namespace DBase;

public interface IMemo : IDisposable, IEnumerable<MemoRecord>
{
    public ushort BlockLength { get; }

    public int NextIndex { get; }

    MemoRecord this[int index] { get; }

    void Flush();

    void Add(MemoRecord record);
}
