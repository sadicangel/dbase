namespace DBase;

/// <summary>
/// Database Language Driver ID.
/// </summary>
public enum DbfLanguage : byte
{
    /// <summary>
    /// Original Equipment Manufacturer (OEM) character set.
    /// </summary>
    Oem = 0,

    /// <summary>
    /// US MS-DOS code page 437.
    /// </summary>
    UsMsDos437 = 0x1,

    /// <summary>
    /// International MS-DOS code page 850.
    /// </summary>
    InternationalMsDos850 = 0x2,

    /// <summary>
    /// Windows ANSI code page 1252.
    /// </summary>
    WindowsAnsi1252 = 0x3,

    /// <summary>
    /// ANSI character set.
    /// </summary>
    Ansi = 0x57,

    /// <summary>
    /// Greek MS-DOS code page 737.
    /// </summary>
    GreekMsDos737 = 0x6a,

    /// <summary>
    /// Eastern European MS-DOS code page 852.
    /// </summary>
    EasternEuropeanMsDos852 = 0x64,

    /// <summary>
    /// Turkish MS-DOS code page 857.
    /// </summary>
    TurkishMsDos857 = 0x6b,

    /// <summary>
    /// Icelandic MS-DOS code page 861.
    /// </summary>
    IcelandicMsDos861 = 0x67,

    /// <summary>
    /// Nordic MS-DOS code page 865.
    /// </summary>
    NordicMsDos865 = 0x66,

    /// <summary>
    /// Russian MS-DOS code page 866.
    /// </summary>
    RussianMsDos866 = 0x65,

    /// <summary>
    /// Chinese Windows code page 950.
    /// </summary>
    ChineseWindows950 = 0x78,

    /// <summary>
    /// Chinese Windows code page 936.
    /// </summary>
    ChineseWindows936 = 0x7a,

    /// <summary>
    /// Japanese Windows code page 932.
    /// </summary>
    JapaneseWindows932 = 0x7b,

    /// <summary>
    /// Hebrew Windows code page 1255.
    /// </summary>
    HebrewWindows1255 = 0x7d,

    /// <summary>
    /// Arabic Windows code page 1256.
    /// </summary>
    ArabicWindows1256 = 0x7e,

    /// <summary>
    /// Eastern European Windows code page 1250.
    /// </summary>
    EasternEuropeanWindows1250 = 0xc8,

    /// <summary>
    /// Russian Windows code page 1251.
    /// </summary>
    RussianWindows1251 = 0xc9,

    /// <summary>
    /// Turkish Windows code page 1254.
    /// </summary>
    TurkishWindows1254 = 0xca,

    /// <summary>
    /// Greek Windows code page 1253.
    /// </summary>
    GreekWindows1253 = 0xcb
}
