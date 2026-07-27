using System.Collections.Immutable;
using System.Text;

namespace DBase.Tests;

public class DbfMemoPolicyTests
{
    public static TheoryData<DbfVersion, string> SupportedMemoVersions { get; } = new()
    {
        { DbfVersion.DBase83, "dbt" },
        { DbfVersion.DBase8B, "dbt" },
        { DbfVersion.VisualFoxPro, "fpt" },
        { DbfVersion.VisualFoxProWithAutoIncrement, "fpt" },
        { DbfVersion.VisualFoxProWithVarchar, "fpt" },
        { DbfVersion.FoxPro2WithMemo, "fpt" },
    };

    public static TheoryData<DbfVersion> UnsupportedMemoVersions { get; } =
    [
        DbfVersion.DBase03,
        DbfVersion.DBase04,
        DbfVersion.DBase05,
        DbfVersion.DBaseCB,
    ];

    [Theory]
    [MemberData(nameof(SupportedMemoVersions))]
    public void Create_MemoFieldWithSupportedVersion_CreatesExpectedMemoFile(DbfVersion version, string extension)
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");
        using var dbf = Dbf.Create(dbfPath, CreateMemoDescriptors(), new DbfCreateOptions { Version = version });

        Assert.True(File.Exists(dbfPath));
        Assert.True(File.Exists(Path.ChangeExtension(dbfPath, extension)));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, extension == "dbt" ? "fpt" : "dbt")));
    }

    [Theory]
    [MemberData(nameof(UnsupportedMemoVersions))]
    public void Create_MemoFieldWithUnsupportedVersion_ThrowsBeforeCreatingFiles(DbfVersion version)
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        var exception = Assert.Throws<NotSupportedException>(
            () => Dbf.Create(dbfPath, CreateMemoDescriptors(), new DbfCreateOptions { Version = version }));

        Assert.Contains("supported memo file format", exception.Message);
        Assert.False(File.Exists(dbfPath));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "dbt")));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "fpt")));
    }

    [Fact]
    public void MemoCreate_UnsupportedVersion_ThrowsBeforeCreatingFile()
    {
        using var temp = new TempDirectory();
        var memoPath = Path.Combine(temp.Path, "table.dbt");

        var exception = Assert.Throws<NotSupportedException>(() => Memo.Create(memoPath, DbfVersion.DBase03));

        Assert.Contains("supported memo file format", exception.Message);
        Assert.False(File.Exists(memoPath));
    }

    [Fact]
    public void SaveAs_FoxPro2WithMemo_CreatesFptMemoThatOpenReads()
    {
        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "source.dbf");
        var savedPath = Path.Combine(temp.Path, "saved.dbf");

        using (var source = Dbf.Create(
            sourcePath,
            CreateMemoDescriptors(),
            new DbfCreateOptions { Version = DbfVersion.FoxPro2WithMemo }))
        {
            source.Add(new DbfRecord((DbfField)"FoxPro 2 memo"));
            source.SaveAs(savedPath);
        }

        var fptPath = Path.ChangeExtension(savedPath, "fpt");
        var dbtPath = Path.ChangeExtension(savedPath, "dbt");

        Assert.True(File.Exists(fptPath));
        Assert.False(File.Exists(dbtPath));

        File.WriteAllBytes(dbtPath, []);

        using var saved = Dbf.Open(savedPath);

        Assert.NotNull(saved.Memo);
        Assert.Equal("FoxPro 2 memo", saved.GetRecord(0)[0].GetValue<string>());
    }

    [Fact]
    public void MemoSave_CopiesFlushedHeaderAndMemoRecords()
    {
        using var temp = new TempDirectory();
        var sourcePath = Path.Combine(temp.Path, "source.dbt");
        var savedPath = Path.Combine(temp.Path, "saved.dbt");
        var text = Encoding.ASCII.GetBytes("standalone memo");

        using (var memo = Memo.Create(sourcePath, DbfVersion.DBase83))
        {
            memo.Add(MemoRecordType.Memo, text);
            memo.Save(savedPath);
        }

        using var saved = Memo.Open(savedPath, DbfVersion.DBase83);
        var record = saved[saved.FirstIndex];

        Assert.Equal(MemoRecordType.Memo, record.Type);
        Assert.Equal(text, record.Span.ToArray());
    }

    private static ImmutableArray<DbfFieldDescriptor> CreateMemoDescriptors() =>
        [DbfFieldDescriptor.Memo(new DbfFieldName("NOTES"))];

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(nameof(DbfMemoPolicyTests)).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
