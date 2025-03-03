using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace DBase.Interop;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct FptHeader
{
    internal const int Size = 8;

    [FieldOffset(0)]
    private readonly int _nextIndex;
    public int NextIndex { get => BinaryPrimitives.ReverseEndianness(_nextIndex); init => _nextIndex = BinaryPrimitives.ReverseEndianness(value); }

    [FieldOffset(4)]
    private readonly ushort _reserved2;

    [FieldOffset(6)]
    private readonly ushort _blockLength;
    public readonly ushort BlockLength { get => BinaryPrimitives.ReverseEndianness(_blockLength); init => _blockLength = BinaryPrimitives.ReverseEndianness(value); }

    [FieldOffset(8)]
    private readonly byte _reserved1;
}
