using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Internal;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct FptHeader
{
    internal const int Size = 8;

    internal const ushort HeaderLengthInDisk = 512;

    [FieldOffset(0)]
    private readonly Int32BigEndian _nextIndex;
    public int NextIndex { get => _nextIndex.Value; init => _nextIndex.Value = value; }

    [FieldOffset(4)]
    private readonly ushort _reserved2;

    [FieldOffset(6)]
    private readonly UInt16BigEndian _blockLength;
    public readonly ushort BlockLength { get => _blockLength.Value; init => _blockLength.Value = value; }

    [FieldOffset(8)]
    private readonly byte _reserved1;

    [InlineArray(4)]
    private struct Int32BigEndian
    {
#pragma warning disable IDE0051 // Remove unused private members
        private byte _e0;
#pragma warning restore IDE0051 // Remove unused private members

        public int Value
        {
            readonly get => BinaryPrimitives.ReadInt32BigEndian(this);
            set => BinaryPrimitives.WriteInt32BigEndian(this, value);
        }
    }

    [InlineArray(2)]
    private struct UInt16BigEndian
    {
#pragma warning disable IDE0051 // Remove unused private members
        private byte _e0;
#pragma warning restore IDE0051 // Remove unused private members

        public ushort Value
        {
            readonly get => BinaryPrimitives.ReadUInt16BigEndian(this);
            set => BinaryPrimitives.WriteUInt16BigEndian(this, value);
        }
    }
}
