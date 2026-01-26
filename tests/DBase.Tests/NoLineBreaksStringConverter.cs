using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DBase.Tests;

internal sealed class NoLineBreaksStringConverter : TypeConverter<string>
{
    private static readonly StringConverter s_stringConverter = new();

    public static readonly NoLineBreaksStringConverter Instance = new();

    private NoLineBreaksStringConverter() { }

    public override string? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
        (string?)s_stringConverter.ConvertFromString(text, row, memberMapData);

    public override string? ConvertToString(string? value, IWriterRow row, MemberMapData memberMapData) =>
        s_stringConverter.ConvertToString(value, row, memberMapData)?.ReplaceLineEndings("    ");
}
