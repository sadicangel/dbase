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

    public DateOnly LastUpdate
    {
        get
        {
            var year = 1900 + LastUpdateYear;
            var month = int.Clamp(LastUpdateMonth, 1, 12);
            var day = int.Clamp(LastUpdateDay, 1, DateTime.DaysInMonth(year, month));
            return new DateOnly(year, month, day);
        }
        init
        {
            LastUpdateYear = (byte)(value.Year - 1900);
            LastUpdateMonth = (byte)value.Month;
            LastUpdateDay = (byte)value.Day;
        }
    }

    public static implicit operator DbfHeader(DbfHeader02 header) => new()
    {
        Version = header.Version,
        RecordCount = header.RecordCount,
        LastUpdate = header.LastUpdate,
        RecordLength = header.RecordLength,
        HeaderLength = HeaderLengthInDisk,
    };

    public static explicit operator DbfHeader02(DbfHeader header)
    {
        if (header.HeaderLength > HeaderLengthInDisk)
        {
            throw new ArgumentException(
                $"Header length {header.HeaderLength} exceeds the dBase II header size of {HeaderLengthInDisk} bytes.",
                nameof(header));
        }

        return new DbfHeader02
        {
            Version = header.Version,
            RecordCount = (ushort)header.RecordCount,
            LastUpdate = header.LastUpdate,
            RecordLength = header.RecordLength,
        };
    }
}
