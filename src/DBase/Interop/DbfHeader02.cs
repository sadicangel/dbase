using System.Runtime.InteropServices;

namespace DBase.Interop;

[StructLayout(LayoutKind.Explicit, Size = Size)]
internal readonly record struct DbfHeader02
{
    internal const int Size = 8;

    internal const int HeaderLengthInDisk = 521;

    [field: FieldOffset(0)] public DbfVersion Version { get; init; }

    [field: FieldOffset(1)] public ushort RecordCount { get; init; }

    [field: FieldOffset(3)] public byte LastUpdateYear { get; init; }

    [field: FieldOffset(4)] public byte LastUpdateMonth { get; init; }

    [field: FieldOffset(5)] public byte LastUpdateDay { get; init; }

    [field: FieldOffset(6)] public ushort RecordLength { get; init; }

    public static implicit operator DbfHeader(DbfHeader02 header) => new()
    {
        Version = header.Version,
        RecordCount = header.RecordCount,
        LastUpdateYear = header.LastUpdateYear,
        LastUpdateMonth = header.LastUpdateMonth,
        LastUpdateDay = header.LastUpdateDay,
        RecordLength = header.RecordLength,
        HeaderLength = HeaderLengthInDisk,
    };

    public static implicit operator DbfHeader02(DbfHeader header) => new()
    {
        Version = header.Version,
        RecordCount = (ushort)header.RecordCount,
        LastUpdateYear = header.LastUpdateYear,
        LastUpdateMonth = header.LastUpdateMonth,
        LastUpdateDay = header.LastUpdateDay,
        RecordLength = header.RecordLength,
    };
}
