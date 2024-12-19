namespace DBase;

/// <summary>
/// Database Language Driver ID.
/// </summary>
public enum DbfLanguage : byte
{
    /// <summary>
    /// Original Equipment Manufacturer (OEM) character set.
    /// </summary>
    OEM = 0,

    /// <summary>
    /// US MSDOS code page 437.
    /// </summary>
    US_MSDOS_437 = 0x1,

    /// <summary>
    /// International MSDOS code page 850.
    /// </summary>
    International_MSDOS_850 = 0x2,

    /// <summary>
    /// Windows ANSI code page 1252.
    /// </summary>
    Windows_ANSI_1252 = 0x3,

    /// <summary>
    /// ANSI character set.
    /// </summary>
    ANSI = 0x57,

    /// <summary>
    /// Greek MSDOS code page 737.
    /// </summary>
    Greek_MSDOS_737 = 0x6a,

    /// <summary>
    /// Eastern European MSDOS code page 852.
    /// </summary>
    Eastern_European_MSDOS_852 = 0x64,

    /// <summary>
    /// Turkish MSDOS code page 857.
    /// </summary>
    Turkish_MSDOS_857 = 0x6b,

    /// <summary>
    /// Icelandic MSDOS code page 861.
    /// </summary>
    Icelandic_MSDOS_861 = 0x67,

    /// <summary>
    /// Nordic MSDOS code page 865.
    /// </summary>
    Nordic_MSDOS_865 = 0x66,

    /// <summary>
    /// Russian MSDOS code page 866.
    /// </summary>
    Russian_MSDOS_866 = 0x65,

    /// <summary>
    /// Chinese Windows code page 950.
    /// </summary>
    Chinese_Windows_950 = 0x78,

    /// <summary>
    /// Chinese Windows code page 936.
    /// </summary>
    Chinese_Windows_936 = 0x7a,

    /// <summary>
    /// Japanese Windows code page 932.
    /// </summary>
    Japanese_Windows_932 = 0x7b,

    /// <summary>
    /// Hebrew Windows code page 1255.
    /// </summary>
    Hebrew_Windows_1255 = 0x7d,

    /// <summary>
    /// Arabic Windows code page 1256.
    /// </summary>
    Arabic_Windows_1256 = 0x7e,

    /// <summary>
    /// Eastern European Windows code page 1250.
    /// </summary>
    Eastern_European_Windows_1250 = 0xc8,

    /// <summary>
    /// Russian Windows code page 1251.
    /// </summary>
    Russian_Windows_1251 = 0xc9,

    /// <summary>
    /// Turkish Windows code page 1254.
    /// </summary>
    Turkish_Windows_1254 = 0xca,

    /// <summary>
    /// Greek Windows code page 1253.
    /// </summary>
    Greek_Windows_1253 = 0xcb
}
