using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase;

/// <summary>
/// Represents the header of a dBASE file.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly record struct DbfHeader
{
    internal const int Size = 32;

    /// <summary>
    /// Gets or sets the version of the dBASE file.
    /// </summary>
    [field: FieldOffset(0)]
    public DbfVersion Version { get; init; }

    [FieldOffset(1)] private readonly byte _lastUpdateYear;
    [FieldOffset(2)] private readonly byte _lastUpdateMonth;
    [FieldOffset(3)] private readonly byte _lastUpdateDay;

    /// <summary>
    /// Gets or sets the date of the last update.
    /// </summary>
    public DateOnly LastUpdate
    {
        get
        {
            var year = 1900 + _lastUpdateYear;
            var month = int.Clamp(_lastUpdateMonth, 1, 12);
            var day = int.Clamp(_lastUpdateDay, 1, DateTime.DaysInMonth(year, month));
            return new DateOnly(year, month, day);
        }
        init
        {
            _lastUpdateYear = (byte)(value.Year - 1900);
            _lastUpdateMonth = (byte)value.Month;
            _lastUpdateDay = (byte)value.Day;
        }
    }

    /// <summary>
    /// Gets or sets the number of records in the dBASE file. 
    /// </summary>
    [field: FieldOffset(4)]
    public uint RecordCount { get; init; }

    /// <summary>
    /// Gets or sets the length of the header structure.
    /// </summary>
    [field: FieldOffset(8)]
    public ushort HeaderLength { get; init; }

    /// <summary>
    /// Gets or sets the length of each record.
    /// </summary>
    [field: FieldOffset(10)]
    public ushort RecordLength { get; init; }

    [FieldOffset(12)] private readonly Reserved16 _reserved16;

    [field: FieldOffset(14)] internal readonly byte TransactionFlag;

    [field: FieldOffset(15)] internal readonly byte EncryptionFlag;

    /// <summary>
    /// Gets or sets the DBF table flags.
    /// </summary>
    [field: FieldOffset(28)]
    public DbfTableFlags TableFlags { get; init; }

    /// <summary>
    /// Gets or sets the language driver ID.
    /// </summary>
    [field: FieldOffset(29)]
    public DbfLanguage Language { get; init; }

    [FieldOffset(30)] private readonly ushort _reserved2;

    [InlineArray(16)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "InlineArray")]
    private struct Reserved16
    {
        private byte _e0;
    }
}
