using System.Runtime.InteropServices;

namespace DBase;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly record struct DbtHeader
{
    internal const int Size = 24;

    internal const ushort HeaderLengthInDisk = 512;

    [field: FieldOffset(0)]
    public int NextBlock { get; init; }

    [FieldOffset(4)]
    private readonly ushort _blockLength03;

    [FieldOffset(6)]
    private readonly ushort _reserved2_1;

    [FieldOffset(8)]
    private readonly long _reserved8;

    [FieldOffset(16)]
    private readonly int _reserved4;

    [FieldOffset(16)]
    private readonly byte _version;

    [FieldOffset(20)]
    private readonly ushort _blockLength04;

    [FieldOffset(22)]
    private readonly ushort _reserved2_2;

    public ushort BlockLength
    {
        get
        {
            var blockLength = _version is 0 ? _blockLength04 : _blockLength03;
            return blockLength > 0 ? blockLength : HeaderLengthInDisk; // Default to 512, same as header size in disk.
        }
        init
        {
            _blockLength03 = value;
            _blockLength04 = value;
        }
    }
}
