namespace DBase;

internal static class SpanChunkEnumerableExtensions
{
    public static SpanChunkEnumerable<TSource> Chunk<TSource>(this ReadOnlySpan<TSource> source, int size) =>
        new(source, size);

    public static SpanChunkEnumerable<TSource> Chunk<TSource>(this Span<TSource> source, int size) =>
        new(source, size);
}

internal readonly ref struct SpanChunkEnumerable<TSource>
{
    private readonly ReadOnlySpan<TSource> _source;
    private readonly int _size;

    public SpanChunkEnumerable(ReadOnlySpan<TSource> source, int size)
    {
        _source = source;
        _size = size;
    }

    public SpanChunkEnumerator GetEnumerator() => new(_source, _size);

    public ref struct SpanChunkEnumerator
    {
        private readonly ReadOnlySpan<TSource> _source;
        private readonly int _size;
        private int _index;

        public SpanChunkEnumerator(ReadOnlySpan<TSource> source, int size)
        {
            _source = source;
            _size = size;
            _index = 0;
        }

        public readonly ReadOnlySpan<TSource> Current =>
            _source.Slice(_index, Math.Min(_size, _source.Length - _index));

        public bool MoveNext()
        {
            if (_index < _source.Length)
            {
                _index += Current.Length;
                return true;
            }
            return false;
        }
    }
}
