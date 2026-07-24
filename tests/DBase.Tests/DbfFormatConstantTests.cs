namespace DBase.Tests;

public sealed class DbfFormatConstantTests
{
    [Fact]
    public void DbfVersion_FormatMarkers_MatchOnDiskValues()
    {
        Assert.Equal(0x43, (byte)DbfVersion.DBase43);
        Assert.Equal(0x63, (byte)DbfVersion.DBase63);
        Assert.Equal(0xFB, (byte)DbfVersion.FoxBASE);
    }

    [Fact]
    public void TableFlags_FormatValues_MatchHeaderBits()
    {
        Assert.Equal(0x04, (byte)DbfTableFlags.IsDbc);
    }

    [Fact]
    public void VersionBitHelpers_ReadDocumentedHeaderBits()
    {
        Assert.True(DbfVersion.DBase8B.HasDosMemo());
        Assert.True(DbfVersion.DBase83.HasDbtMemo());
        Assert.True(DbfVersion.DBase43.HasSqlTable());

        Assert.False(DbfVersion.DBase03.HasDosMemo());
        Assert.False(DbfVersion.DBase03.HasDbtMemo());
        Assert.False(DbfVersion.DBase03.HasSqlTable());
    }
}
