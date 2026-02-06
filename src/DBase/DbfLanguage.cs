namespace DBase;

/// <summary>
/// Identifies the DBF language-driver marker stored in the file header.
/// </summary>
/// <remarks>
/// The value is a single byte used by DBF producers/consumers to select text encoding and locale-specific
/// formatting behavior (for example decimal separator). In this library, mappings are implemented in
/// <see cref="DbfLanguageExtensions"/>.
/// </remarks>
public enum DbfLanguage : byte
{
    /// <summary>
    /// OEM/default marker (<c>0x00</c>).
    /// </summary>
    Oem = 0,

    /// <summary>
    /// US MS-DOS code page 437 (<c>0x01</c>).
    /// </summary>
    UsMsDos437 = 0x1,

    /// <summary>
    /// International MS-DOS code page 850 (<c>0x02</c>).
    /// </summary>
    InternationalMsDos850 = 0x2,

    /// <summary>
    /// Windows ANSI code page 1252 (<c>0x03</c>).
    /// </summary>
    WindowsAnsi1252 = 0x3,

    /// <summary>
    /// ANSI/default marker used by some DBF variants (<c>0x57</c>).
    /// </summary>
    Ansi = 0x57,

    /// <summary>
    /// Greek MS-DOS code page 737 (<c>0x6A</c>).
    /// </summary>
    GreekMsDos737 = 0x6a,

    /// <summary>
    /// Eastern European MS-DOS code page 852 (<c>0x64</c>).
    /// </summary>
    EasternEuropeanMsDos852 = 0x64,

    /// <summary>
    /// Turkish MS-DOS code page 857 (<c>0x6B</c>).
    /// </summary>
    TurkishMsDos857 = 0x6b,

    /// <summary>
    /// Icelandic MS-DOS code page 861 (<c>0x67</c>).
    /// </summary>
    IcelandicMsDos861 = 0x67,

    /// <summary>
    /// Nordic MS-DOS code page 865 (<c>0x66</c>).
    /// </summary>
    NordicMsDos865 = 0x66,

    /// <summary>
    /// Russian MS-DOS code page 866 (<c>0x65</c>).
    /// </summary>
    RussianMsDos866 = 0x65,

    /// <summary>
    /// Traditional Chinese Windows code page 950 (<c>0x78</c>).
    /// </summary>
    ChineseWindows950 = 0x78,

    /// <summary>
    /// Simplified Chinese Windows code page 936 (<c>0x7A</c>).
    /// </summary>
    ChineseWindows936 = 0x7a,

    /// <summary>
    /// Japanese Windows code page 932 (<c>0x7B</c>).
    /// </summary>
    JapaneseWindows932 = 0x7b,

    /// <summary>
    /// Hebrew Windows code page 1255 (<c>0x7D</c>).
    /// </summary>
    HebrewWindows1255 = 0x7d,

    /// <summary>
    /// Arabic Windows code page 1256 (<c>0x7E</c>).
    /// </summary>
    ArabicWindows1256 = 0x7e,

    /// <summary>
    /// Eastern European Windows code page 1250 (<c>0xC8</c>).
    /// </summary>
    EasternEuropeanWindows1250 = 0xc8,

    /// <summary>
    /// Russian Windows code page 1251 (<c>0xC9</c>).
    /// </summary>
    RussianWindows1251 = 0xc9,

    /// <summary>
    /// Turkish Windows code page 1254 (<c>0xCA</c>).
    /// </summary>
    TurkishWindows1254 = 0xca,

    /// <summary>
    /// Greek Windows code page 1253 (<c>0xCB</c>).
    /// </summary>
    GreekWindows1253 = 0xcb
}
