using System.Globalization;
using System.Text;

namespace DBase;

internal static class DbfEncoding
{
    static DbfEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    internal static Encoding DefaultFallbackEncoding => Encoding.GetEncoding(437);

    internal static Encoding Resolve(DbfLanguage language, Encoding fallbackEncoding) =>
        DbfLanguageExtensions.TryGetEncoding(language) ?? fallbackEncoding;

    internal static Encoding? TryReadCodePageFile(string dbfFileName)
    {
        var cpgFileName = Path.ChangeExtension(dbfFileName, ".cpg");
        if (!File.Exists(cpgFileName))
        {
            return null;
        }

        var value = File.ReadAllText(cpgFileName).AsSpan().Trim().Trim('\uFEFF').Trim();
        return TryGetEncoding(value);
    }

    private static Encoding? TryGetEncoding(ReadOnlySpan<char> value)
    {
        if (value.IsWhiteSpace())
        {
            return null;
        }

        if (value.Equals("ANSI", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.GetEncoding(1252);
        }

        if (value.Equals("OEM", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.GetEncoding(437);
        }

        if (value.StartsWith("CP", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        try
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var codePage)
                ? Encoding.GetEncoding(codePage)
                : Encoding.GetEncoding(value.ToString());
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}
