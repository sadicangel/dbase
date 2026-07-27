using System.Collections.Immutable;

namespace DBase.Tests;

public sealed class DbfVersionInferenceTests
{
    public static TheoryData<ImmutableArray<DbfFieldDescriptor>, DbfVersion> InferredVersions { get; } = new()
    {
        {
            Fields(
                DbfFieldDescriptor.Character("NAME", 20),
                DbfFieldDescriptor.Numeric("AGE"),
                DbfFieldDescriptor.Date("BORN"),
                DbfFieldDescriptor.Logical("ACTIVE")),
            DbfVersion.DBase03
        },
        { Fields(DbfFieldDescriptor.Memo("NOTES")), DbfVersion.DBase83 },
        { Fields(DbfFieldDescriptor.Float("SCORE", 10, 2)), DbfVersion.DBase04 },
        { Fields(DbfFieldDescriptor.Float("SCORE", 10, 2), DbfFieldDescriptor.Memo("NOTES")), DbfVersion.DBase8B },
        { Fields(DbfFieldDescriptor.Currency("AMOUNT")), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.DateTime("UPDATED")), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Int32("COUNT")), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Binary("DOUBLE", 8)), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Memo("NOTES", 4)), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Ole("OBJECT")), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Picture("IMAGE")), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.NullFlags("_NullFlags", 1)), DbfVersion.VisualFoxPro },
        { Fields(DbfFieldDescriptor.Variant("NAME", 20)), DbfVersion.VisualFoxProWithVarchar },
        { Fields(DbfFieldDescriptor.Blob("PAYLOAD")), DbfVersion.VisualFoxProWithVarchar },
        { Fields(DbfFieldDescriptor.Int32("ID") with { Flags = DbfFieldFlags.AutoIncrement }), DbfVersion.VisualFoxProWithAutoIncrement },
    };

    public static TheoryData<ImmutableArray<DbfFieldDescriptor>> UnsupportedInferredDescriptors { get; } = new()
    {
        { Fields(DbfFieldDescriptor.AutoIncrement("ID")) },
        { Fields(DbfFieldDescriptor.Double("VALUE")) },
        { Fields(DbfFieldDescriptor.Timestamp("UPDATED")) },
        { Fields(DbfFieldDescriptor.Binary("PAYLOAD")) },
    };

    [Fact]
    public void DbfVersion_Unspecified_UsesZeroMarker()
    {
        Assert.Equal(0, (byte)DbfVersion.Unspecified);
    }

    [Theory]
    [MemberData(nameof(InferredVersions))]
    public void Create_UnspecifiedVersion_InfersExpectedVersion(
        ImmutableArray<DbfFieldDescriptor> descriptors,
        DbfVersion expectedVersion)
    {
        using var dbf = Dbf.Create(
            new MemoryStream(),
            descriptors,
            HasMemoBackedFields(descriptors) ? new MemoryStream() : null);

        Assert.Equal(expectedVersion, dbf.Version);
    }

    [Fact]
    public void Create_UnspecifiedClassicMemo_CreatesDbtMemoFile()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create(dbfPath, Fields(DbfFieldDescriptor.Memo("NOTES")));

        Assert.Equal(DbfVersion.DBase83, dbf.Version);
        Assert.True(File.Exists(dbfPath));
        Assert.True(File.Exists(Path.ChangeExtension(dbfPath, "dbt")));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "fpt")));
    }

    [Fact]
    public void Create_UnspecifiedFoxProObjectMemo_CreatesFptMemoFile()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create(dbfPath, Fields(DbfFieldDescriptor.Ole("OBJECT")));

        Assert.Equal(DbfVersion.VisualFoxPro, dbf.Version);
        Assert.True(File.Exists(dbfPath));
        Assert.True(File.Exists(Path.ChangeExtension(dbfPath, "fpt")));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "dbt")));
    }

    [Theory]
    [MemberData(nameof(UnsupportedInferredDescriptors))]
    public void Create_UnspecifiedUnsupportedDescriptors_ThrowsBeforeCreatingFiles(
        ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        var exception = Assert.Throws<NotSupportedException>(() => Dbf.Create(dbfPath, descriptors));

        Assert.Contains("Cannot infer a supported DBF version", exception.Message);
        Assert.False(File.Exists(dbfPath));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "dbt")));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "fpt")));
    }

    [Fact]
    public void Create_ExplicitVersion_DoesNotPromoteDescriptorSet()
    {
        using var dbf = Dbf.Create(
            new MemoryStream(),
            Fields(DbfFieldDescriptor.Currency("AMOUNT")),
            options: new DbfCreateOptions { Version = DbfVersion.DBase03 });

        Assert.Equal(DbfVersion.DBase03, dbf.Version);
    }

    private static ImmutableArray<DbfFieldDescriptor> Fields(params DbfFieldDescriptor[] descriptors) =>
        [.. descriptors];

    private static bool HasMemoBackedFields(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type is DbfFieldType.Blob or DbfFieldType.Memo or DbfFieldType.Ole or DbfFieldType.Picture ||
                descriptor.Type is DbfFieldType.Binary && descriptor.Length is not 8)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(nameof(DbfVersionInferenceTests)).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
