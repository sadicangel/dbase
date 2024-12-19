using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly record struct DbfHeader
{
    internal const int Size = 32;

    [field: FieldOffset(0)]
    public DbfVersion Version { get; init; }

    [field: FieldOffset(1)]
    public byte LastUpdateYear { get; init; }

    [field: FieldOffset(2)]
    public byte LastUpdateMonth { get; init; }

    [field: FieldOffset(3)]
    public byte LastUpdateDay { get; init; }

    [field: FieldOffset(4)]
    public uint RecordCount { get; init; }

    [field: FieldOffset(8)]
    public ushort HeaderLength { get; init; }

    [field: FieldOffset(10)]
    public ushort RecordLength { get; init; }

    [FieldOffset(12)]
    private readonly Reserved16 _reserved16;

    [field: FieldOffset(14)]
    internal readonly byte TransactionFlag;

    [field: FieldOffset(15)]
    internal readonly byte EncryptionFlag;

    [field: FieldOffset(28)]
    public DbfTableFlags TableFlags { get; init; }

    [field: FieldOffset(29)]
    public DbfLanguage Language { get; init; }

    [FieldOffset(30)]
    private readonly ushort _reserved2;

    public DateTime LastUpdate
    {
        get => new(1900 + LastUpdateYear, LastUpdateMonth, LastUpdateDay);
        init => (LastUpdateYear, LastUpdateMonth, LastUpdateDay) = ((byte)(value.Year - 1900), (byte)value.Month, (byte)value.Day);
    }

    [InlineArray(16)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "InlineArray")]
    private struct Reserved16 { private byte _e0; }
}
