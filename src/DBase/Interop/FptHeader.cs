using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace DBase.Interop;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct FptHeader
{
    internal const int Size = 8;

    [field: FieldOffset(0)]
    public int NextIndex
    {
        get => BinaryPrimitives.ReverseEndianness(field);
        init => field = BinaryPrimitives.ReverseEndianness(value);
    }

    [FieldOffset(4)] private readonly ushort _reserved2;

    [field: FieldOffset(6)]
    public ushort BlockLength
    {
        get => BinaryPrimitives.ReverseEndianness(field);
        init => field = BinaryPrimitives.ReverseEndianness(value);
    }

    [FieldOffset(8)] private readonly byte _reserved1;
}
