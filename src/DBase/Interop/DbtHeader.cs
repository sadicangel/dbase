﻿using System.Runtime.InteropServices;

namespace DBase.Interop;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct DbtHeader
{
    internal const int Size = 24;

    [field: FieldOffset(0)]
    public int NextIndex { get; init; }

    [FieldOffset(4)]
    private readonly ushort _blockLength03;

    [FieldOffset(6)]
    private readonly ushort _reserved2_1;

    [FieldOffset(8)]
    private readonly long _reserved8;

    [FieldOffset(16)]
    private readonly int _reserved4;

    [field: FieldOffset(16)]
    public byte Version { get; init; }

    [FieldOffset(20)]
    private readonly ushort _blockLength04;

    [FieldOffset(22)]
    private readonly ushort _reserved2_2;

    public ushort BlockLength
    {
        get
        {
            var blockLength = Version is 0 ? _blockLength04 : _blockLength03;
            return blockLength > 0 ? blockLength : Memo.HeaderLengthInDisk; // Default to 512, same as header size in disk.
        }
        init
        {
            _blockLength03 = value;
            _blockLength04 = value;
        }
    }
}
