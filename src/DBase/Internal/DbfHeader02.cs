using System.Runtime.InteropServices;

namespace DBase.Internal;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct DbfHeader02
{
    internal const int Size = 8;

    [FieldOffset(0)]
    public readonly DbfVersion Version;

    [FieldOffset(1)]
    public readonly ushort RecordCount;

    [FieldOffset(3)]
    public readonly byte LastUpdateYear;

    [FieldOffset(4)]
    public readonly byte LastUpdateMonth;

    [FieldOffset(5)]
    public readonly byte LastUpdateDay;

    [FieldOffset(6)]
    public readonly ushort RecordLength;

    public static implicit operator DbfHeader(DbfHeader02 header) => new()
    {
        Version = header.Version,
        RecordCount = header.RecordCount,
        LastUpdateYear = header.LastUpdateYear,
        LastUpdateMonth = header.LastUpdateMonth,
        LastUpdateDay = header.LastUpdateDay,
        RecordLength = header.RecordLength,
        HeaderLength = 521,
    };
}
