using System.Collections.Immutable;
using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests;

public sealed class DbfEncodingTests
{
    public static TheoryData<DbfLanguage, int> LanguageEncodings { get; } = new()
    {
        { DbfLanguage.Oem, 437 },
        { DbfLanguage.UsMsDos437, 437 },
        { DbfLanguage.InternationalMsDos850, 850 },
        { DbfLanguage.Ansi, 1252 },
        { DbfLanguage.WindowsAnsi1252, 1252 },
    };

    [Theory]
    [MemberData(nameof(LanguageEncodings))]
    public void GetEncoding_LanguageMarker_MapsToExpectedEncoding(DbfLanguage language, int expectedCodePage)
    {
        Assert.Equal(expectedCodePage, language.GetEncoding().CodePage);
    }

    [Fact]
    public void Open_CpgSidecar_OverridesLanguageMarker()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");
        var cp1252 = DbfLanguage.WindowsAnsi1252.GetEncoding();

        CreateSingleTextRecord(dbfPath, DbfLanguage.UsMsDos437, cp1252, "é");
        File.WriteAllText(Path.ChangeExtension(dbfPath, ".cpg"), "1252");

        using var dbf = Dbf.Open(dbfPath);

        Assert.Equal(1252, dbf.Encoding.CodePage);
        Assert.Equal("é", dbf.GetRecord(0)[0].GetValue<string>());
    }

    [Fact]
    public void Open_ExplicitEncoding_OverridesCpgSidecarAndLanguageMarker()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");
        var cp1252 = DbfLanguage.WindowsAnsi1252.GetEncoding();

        CreateSingleTextRecord(dbfPath, DbfLanguage.UsMsDos437, cp1252, "é");
        File.WriteAllText(Path.ChangeExtension(dbfPath, ".cpg"), "437");

        using var dbf = Dbf.Open(dbfPath, new DbfOpenOptions { Encoding = cp1252 });

        Assert.Equal(1252, dbf.Encoding.CodePage);
        Assert.Equal("é", dbf.GetRecord(0)[0].GetValue<string>());
    }

    [Fact]
    public void CharacterWriteRaw_TextTooWide_TruncatesByEncodedByteWidthAndPadsRemaining()
    {
        var encoding = Encoding.UTF8;
        Span<byte> truncated = stackalloc byte[6];
        Span<byte> padded = stackalloc byte[6];

        DbfFieldCharacterFormatter.WriteRaw(truncated, "éabcde", encoding);
        DbfFieldCharacterFormatter.WriteRaw(padded, "éab", encoding);

        Assert.Equal([0xC3, 0xA9, (byte)'a', (byte)'b', (byte)'c', (byte)'d'], truncated.ToArray());
        Assert.Equal([0xC3, 0xA9, (byte)'a', (byte)'b', (byte)' ', (byte)' '], padded.ToArray());
    }

    [Fact]
    public void CharacterWriteRaw_MultiByteCharacterTooWide_DoesNotEmitPartialCharacter()
    {
        Span<byte> buffer = stackalloc byte[2];

        DbfFieldCharacterFormatter.WriteRaw(buffer, "aé", Encoding.UTF8);

        Assert.Equal([(byte)'a', (byte)' '], buffer.ToArray());
    }

    [Fact]
    public void VariantWriteRaw_MultiByteCharacterTooWide_DoesNotEmitPartialCharacter()
    {
        Span<byte> buffer = stackalloc byte[4];

        DbfFieldVariantFormatter.WriteRaw(buffer, "éé", Encoding.UTF8);

        Assert.Equal([0xC3, 0xA9, (byte)' ', 0x02], buffer.ToArray());
        Assert.Equal("é", DbfFieldVariantFormatter.ReadRaw(buffer, Encoding.UTF8));
    }

    private static void CreateSingleTextRecord(string dbfPath, DbfLanguage language, Encoding encoding, string value)
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 10));
        using var dbf = Dbf.Create(
            dbfPath,
            descriptors,
            new DbfCreateOptions
            {
                Language = language,
                Encoding = encoding,
            });

        dbf.Add(new DbfRecord(value));
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(nameof(DbfEncodingTests)).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
