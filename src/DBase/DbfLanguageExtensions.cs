using System.ComponentModel;
using System.Text;

namespace DBase;

/// <summary>
/// Extensions for <see cref="DbfLanguage" />.
/// </summary>
public static class DbfLanguageExtensions
{
    /// <summary>
    /// Gets the decimal separator <see cref="char" /> for the <see cref="DbfLanguage" />.
    /// </summary>
    /// <param name="language">The language <i>codepage</i>.</param>
    /// <returns></returns>
    public static char GetDecimalSeparator(this DbfLanguage language) => language switch
    {
        DbfLanguage.OEM or
        DbfLanguage.ANSI or
        DbfLanguage.Greek_MSDOS_737 or
        DbfLanguage.Eastern_European_MSDOS_852 or
        DbfLanguage.Greek_Windows_1253 or
        DbfLanguage.Turkish_MSDOS_857 or
        DbfLanguage.Icelandic_MSDOS_861 or
        DbfLanguage.Nordic_MSDOS_865 or
        DbfLanguage.Russian_MSDOS_866 or
        DbfLanguage.Eastern_European_Windows_1250 or
        DbfLanguage.Turkish_Windows_1254
            => ',',
        DbfLanguage.US_MSDOS_437 or
        DbfLanguage.Japanese_Windows_932 or
        DbfLanguage.Chinese_Windows_936 or
        DbfLanguage.Chinese_Windows_950
            => '.',
        DbfLanguage.Windows_ANSI_1252 or
        DbfLanguage.Hebrew_Windows_1255 or
        DbfLanguage.International_MSDOS_850 or
        DbfLanguage.Arabic_Windows_1256
            => '.',
        DbfLanguage.Russian_Windows_1251
            => ' ',
        _
            => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
    };

    /// <summary>
    /// Gets the <see cref="Encoding"/> associated with the <see cref="DbfLanguage" />.
    /// </summary>
    /// <param name="language">The language <i>codepage</i>.</param>
    /// <returns></returns>
    public static Encoding GetEncoding(this DbfLanguage language) => language switch
    {
        DbfLanguage.US_MSDOS_437 => Encoding.GetEncoding("IBM437"),
        DbfLanguage.Greek_MSDOS_737 => Encoding.GetEncoding("ibm737"),
        DbfLanguage.International_MSDOS_850 => Encoding.GetEncoding("ibm850"),
        DbfLanguage.Eastern_European_MSDOS_852 => Encoding.GetEncoding("ibm852"),
        DbfLanguage.Turkish_MSDOS_857 => Encoding.GetEncoding("ibm857"),
        DbfLanguage.Icelandic_MSDOS_861 => Encoding.GetEncoding("ibm861"),
        DbfLanguage.Nordic_MSDOS_865 => Encoding.GetEncoding("IBM865"),
        DbfLanguage.Russian_MSDOS_866 => Encoding.GetEncoding("cp866"),
        DbfLanguage.Japanese_Windows_932 => Encoding.GetEncoding("shift_jis"),
        DbfLanguage.Chinese_Windows_936 => Encoding.GetEncoding("gb2312"),
        DbfLanguage.Chinese_Windows_950 => Encoding.GetEncoding("big5"),
        DbfLanguage.Eastern_European_Windows_1250 => Encoding.GetEncoding("windows-1250"),
        DbfLanguage.Russian_Windows_1251 => Encoding.GetEncoding("windows-1251"),
        DbfLanguage.Windows_ANSI_1252 => Encoding.GetEncoding("windows-1252"),
        DbfLanguage.Greek_Windows_1253 => Encoding.GetEncoding("windows-1253"),
        DbfLanguage.Turkish_Windows_1254 => Encoding.GetEncoding("windows-1254"),
        DbfLanguage.Hebrew_Windows_1255 => Encoding.GetEncoding("windows-1255"),
        DbfLanguage.Arabic_Windows_1256 => Encoding.GetEncoding("windows-1256"),
        DbfLanguage.OEM or DbfLanguage.ANSI => Encoding.ASCII,
        _ => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
    };
}
