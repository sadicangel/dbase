using System.Buffers;
using System.Collections.Immutable;
using System.Text;

namespace DBase;

// TODO: Turn into a class. Support non-const block size.
internal record struct DbfMemoHelper(Stream Stream, Encoding Encoding)
{
    internal const int BlockSize = 512;

    public int NextIndex { get; private set; } = (int)(Stream.Length / BlockSize);

    public readonly string ReadString(int index)
    {
        var builder = new StringBuilder();
        var pooledByteArray = ArrayPool<byte>.Shared.Rent(BlockSize);
        var byteBuffer = pooledByteArray.AsSpan(0, BlockSize);
        var pooledCharArray = ArrayPool<char>.Shared.Rent(Encoding.GetMaxCharCount(BlockSize));
        var charBuffer = pooledCharArray.AsSpan(0, BlockSize);
        Stream.Position = index * BlockSize;
        while (Stream.Position < Stream.Length)
        {
            // Support last block with less than BlockSize bytes.
            var size = (int)Math.Min(Stream.Length - Stream.Position, BlockSize);
            Stream.ReadExactly(byteBuffer[..size]);
            var end = byteBuffer.IndexOf((byte)0x1A);
            if (end >= 0)
            {
                var charsWritten = Encoding.GetChars(byteBuffer[..end], charBuffer);
                builder.Append(charBuffer[..charsWritten]);
                break;
            }
            else
            {
                var charsWritten = Encoding.GetChars(byteBuffer, charBuffer);
                builder.Append(charBuffer[..charsWritten]);
            }
        }
        ArrayPool<byte>.Shared.Return(pooledByteArray);
        ArrayPool<char>.Shared.Return(pooledCharArray);
        return builder.ToString();
    }

    public void WriteString(int index, string value)
    {
        Stream.Position = index * BlockSize;
        var byteCount = Encoding.GetByteCount(value) + 2 /* terminator bytes */;
        var pooledByteArray = ArrayPool<byte>.Shared.Rent(byteCount);
        var byteBuffer = pooledByteArray.AsSpan(0, byteCount);
        Encoding.GetBytes(value, byteBuffer);
        while (byteBuffer.Length > BlockSize)
        {
            Stream.Write(byteBuffer[..BlockSize]);
            byteBuffer = byteBuffer[BlockSize..];
            index++;
        }
        if (byteBuffer.Length > 0)
        {
            Stream.SetLength(Stream.Length + BlockSize);
            Stream.Write(byteBuffer);
            Stream.WriteByte(0x1A);
            Stream.WriteByte(0x1A);
            index++;
            Stream.Position = index * BlockSize;
        }
        NextIndex = index;
        ArrayPool<byte>.Shared.Return(pooledByteArray);
    }
}
