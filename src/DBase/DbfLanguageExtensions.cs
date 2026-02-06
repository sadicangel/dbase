using System.ComponentModel;
using System.Text;

namespace DBase;

/// <summary>
/// Provides mapping helpers for <see cref="DbfLanguage"/> values.
/// </summary>
/// <remarks>
/// These mappings are used by DBF serializers to determine character encoding and numeric formatting rules.
/// </remarks>
public static class DbfLanguageExtensions
{
    static DbfLanguageExtensions()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <param name="language">The DBF language-driver marker.</param>
    extension(DbfLanguage language)
    {
        /// <summary>
        /// Gets the decimal separator used for numeric text fields for the specified language marker.
        /// </summary>
        /// <returns>The decimal separator character.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// language is not a supported <see cref="DbfLanguage"/> value.
        /// </exception>
        public char GetDecimalSeparator() => language switch
        {
            DbfLanguage.Oem or
                DbfLanguage.Ansi or
                DbfLanguage.GreekMsDos737 or
                DbfLanguage.EasternEuropeanMsDos852 or
                DbfLanguage.GreekWindows1253 or
                DbfLanguage.TurkishMsDos857 or
                DbfLanguage.IcelandicMsDos861 or
                DbfLanguage.NordicMsDos865 or
                DbfLanguage.RussianMsDos866 or
                DbfLanguage.EasternEuropeanWindows1250 or
                DbfLanguage.TurkishWindows1254
                => ',',
            DbfLanguage.UsMsDos437 or
                DbfLanguage.JapaneseWindows932 or
                DbfLanguage.ChineseWindows936 or
                DbfLanguage.ChineseWindows950
                => '.',
            DbfLanguage.WindowsAnsi1252 or
                DbfLanguage.HebrewWindows1255 or
                DbfLanguage.InternationalMsDos850 or
                DbfLanguage.ArabicWindows1256
                => '.',
            DbfLanguage.RussianWindows1251
                => ' ',
            _
                => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
        };

        /// <summary>
        /// Gets the text encoding associated with the specified language marker.
        /// </summary>
        /// <returns>The encoding used to read and write character data.</returns>
        /// <exception cref="InvalidEnumArgumentException">
        /// language is not a supported <see cref="DbfLanguage"/> value.
        /// </exception>
        public Encoding GetEncoding() => language switch
        {
            DbfLanguage.UsMsDos437 => Encoding.GetEncoding(437),
            DbfLanguage.GreekMsDos737 => Encoding.GetEncoding(737),
            DbfLanguage.InternationalMsDos850 => Encoding.GetEncoding(850),
            DbfLanguage.EasternEuropeanMsDos852 => Encoding.GetEncoding(852),
            DbfLanguage.TurkishMsDos857 => Encoding.GetEncoding(857),
            DbfLanguage.IcelandicMsDos861 => Encoding.GetEncoding(861),
            DbfLanguage.NordicMsDos865 => Encoding.GetEncoding(865),
            DbfLanguage.RussianMsDos866 => Encoding.GetEncoding(866),
            DbfLanguage.JapaneseWindows932 => Encoding.GetEncoding(932),
            DbfLanguage.ChineseWindows936 => Encoding.GetEncoding(936),
            DbfLanguage.ChineseWindows950 => Encoding.GetEncoding(950),
            DbfLanguage.EasternEuropeanWindows1250 => Encoding.GetEncoding(1250),
            DbfLanguage.RussianWindows1251 => Encoding.GetEncoding(1251),
            DbfLanguage.WindowsAnsi1252 => Encoding.GetEncoding(1252),
            DbfLanguage.GreekWindows1253 => Encoding.GetEncoding(1253),
            DbfLanguage.TurkishWindows1254 => Encoding.GetEncoding(1254),
            DbfLanguage.HebrewWindows1255 => Encoding.GetEncoding(1255),
            DbfLanguage.ArabicWindows1256 => Encoding.GetEncoding(1256),
            DbfLanguage.Oem or DbfLanguage.Ansi => Encoding.ASCII,
            _ => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
        };
    }
}
