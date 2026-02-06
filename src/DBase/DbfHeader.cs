using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DBase.Interop;

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

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
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

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    [FieldOffset(30)] private readonly ushort _reserved2;

    [InlineArray(16)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "InlineArray")]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "InlineArray")]
    private struct Reserved16
    {
        private byte _e0;
    }

    /// <summary>
    /// Initializes a new instance of the DbfHeader class using the specified DBF file version, language, and field
    /// descriptors. This constructor sets up the header information required for a DBF table, including record
    /// structure and metadata.
    /// </summary>
    /// <param name="descriptors">An immutable array of field descriptors that define the schema of the DBF table, including field names, types, and lengths.</param>
    /// <param name="version">The DBF file format version to use for the header. Determines the structure and interpretation of the header fields.</param>
    /// <param name="language">The language driver setting for the DBF file, which specifies the character encoding used for text fields.</param>
    public DbfHeader(ImmutableArray<DbfFieldDescriptor> descriptors, DbfVersion version, DbfLanguage language)
    {
        HeaderLength = version is DbfVersion.DBase02
            ? (ushort)DbfHeader02.HeaderLengthInDisk
            : (ushort)(Size + descriptors.Length * DbfFieldDescriptor.Size + 1);
        if (version.IsFoxPro()) HeaderLength += 263;
        Language = language;
        LastUpdate = DateOnly.FromDateTime(DateTime.Now);
        RecordCount = 0;
        RecordLength = (ushort)(1 + descriptors.Sum(static d => d.Length));
        TableFlags = version.IsFoxPro() ? descriptors.GetTableFlags() : DbfTableFlags.None;
        Version = version;
    }
}
