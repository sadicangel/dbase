using System.Reflection;

namespace DBase.Tests;

public sealed class DbfFieldAttributeTests
{
    private static readonly MethodInfo s_fromProperty =
        typeof(DbfFieldDescriptor).GetMethod(
            "FromProperty",
            BindingFlags.Static | BindingFlags.NonPublic,
            [typeof(PropertyInfo), typeof(DbfVersion)])
        ?? throw new InvalidOperationException("DbfFieldDescriptor.FromProperty was not found.");

    public static TheoryData<string, string, DbfFieldType, int, int, DbfFieldFlags> AttributeDescriptors { get; } = new()
    {
        { nameof(AttributeModel.Character), "ALIAS", DbfFieldType.Character, 20, 0, DbfFieldFlags.Nullable },
        { nameof(AttributeModel.Numeric), nameof(AttributeModel.Numeric), DbfFieldType.Numeric, 8, 2, DbfFieldFlags.None },
        { nameof(AttributeModel.Float), nameof(AttributeModel.Float), DbfFieldType.Float, 9, 3, DbfFieldFlags.None },
        { nameof(AttributeModel.BinaryMemo), nameof(AttributeModel.BinaryMemo), DbfFieldType.Binary, 10, 0, DbfFieldFlags.Binary },
        { nameof(AttributeModel.BinaryDouble), nameof(AttributeModel.BinaryDouble), DbfFieldType.Binary, 8, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Memo), nameof(AttributeModel.Memo), DbfFieldType.Memo, 10, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Ole), nameof(AttributeModel.Ole), DbfFieldType.Ole, 10, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Picture), nameof(AttributeModel.Picture), DbfFieldType.Picture, 10, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.NullFlags), nameof(AttributeModel.NullFlags), DbfFieldType.NullFlags, 1, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Variant), nameof(AttributeModel.Variant), DbfFieldType.Variant, 30, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.AutoIncrement), nameof(AttributeModel.AutoIncrement), DbfFieldType.AutoIncrement, 4, 0, DbfFieldFlags.AutoIncrement },
        { nameof(AttributeModel.Blob), nameof(AttributeModel.Blob), DbfFieldType.Blob, 10, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Currency), nameof(AttributeModel.Currency), DbfFieldType.Currency, 8, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Date), nameof(AttributeModel.Date), DbfFieldType.Date, 8, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.DateTime), nameof(AttributeModel.DateTime), DbfFieldType.DateTime, 8, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Double), nameof(AttributeModel.Double), DbfFieldType.Double, 8, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Int32), nameof(AttributeModel.Int32), DbfFieldType.Int32, 4, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Int32AutoIncrement), nameof(AttributeModel.Int32AutoIncrement), DbfFieldType.Int32, 4, 0, DbfFieldFlags.AutoIncrement },
        { nameof(AttributeModel.Logical), nameof(AttributeModel.Logical), DbfFieldType.Logical, 1, 0, DbfFieldFlags.None },
        { nameof(AttributeModel.Timestamp), nameof(AttributeModel.Timestamp), DbfFieldType.Timestamp, 8, 0, DbfFieldFlags.None },
    };

    [Theory]
    [MemberData(nameof(AttributeDescriptors))]
    public void FromProperty_FieldAttribute_ReturnsExpectedDescriptor(
        string propertyName,
        string expectedName,
        DbfFieldType expectedType,
        int expectedLength,
        int expectedDecimal,
        DbfFieldFlags expectedFlags)
    {
        var descriptor = InvokeFromProperty<AttributeModel>(propertyName);

        Assert.Equal(GetExpectedDbfName(expectedName), descriptor.Name.ToString());
        Assert.Equal(expectedType, descriptor.Type);
        Assert.Equal((byte)expectedLength, descriptor.Length);
        Assert.Equal((byte)expectedDecimal, descriptor.Decimal);
        Assert.Equal(expectedFlags, descriptor.Flags);
    }

    [Fact]
    public void CreateTyped_IgnoredProperty_ExcludesDescriptor()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create<IgnoredPropertyModel>(dbfPath);

        var descriptor = Assert.Single(dbf.Descriptors);
        Assert.Equal(nameof(IgnoredPropertyModel.Name), descriptor.Name.ToString());
    }

    [Fact]
    public void TypedRoundtrip_IgnoredProperty_IsNotSerialized()
    {
        using var source = Dbf.Create(
            new MemoryStream(),
            [DbfFieldDescriptor.Character(nameof(IgnoredPropertyModel.Name), 20)]);

        source.Add(new IgnoredPropertyModel
        {
            Name = "Ada",
            Ignored = "secret"
        });

        var actual = source.GetRecord<IgnoredPropertyModel>(0);

        Assert.Equal("Ada", actual.Name);
        Assert.Equal("default", actual.Ignored);
    }

    [Fact]
    public void CreateTyped_IgnoreAndFieldAttribute_ThrowsArgumentException()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        Assert.Throws<ArgumentException>(() => Dbf.Create<ConflictingIgnoredModel>(dbfPath));

        Assert.False(File.Exists(dbfPath));
    }

    [Fact]
    public void CreateTyped_MultipleFieldAttributes_ThrowsArgumentException()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        Assert.Throws<ArgumentException>(() => Dbf.Create<MultipleFieldAttributesModel>(dbfPath));

        Assert.False(File.Exists(dbfPath));
    }

    [Fact]
    public void CreateTyped_CharacterAttribute_UsesConfiguredLength()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create<TypedCharacterModel>(dbfPath);

        var descriptor = Assert.Single(dbf.Descriptors);
        Assert.Equal(DbfFieldType.Character, descriptor.Type);
        Assert.Equal((byte)20, descriptor.Length);
    }

    [Fact]
    public void CreateTyped_MemoAttribute_InfersDBase83AndCreatesDbt()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create<TypedMemoModel>(dbfPath);

        Assert.Equal(DbfVersion.DBase83, dbf.Version);
        Assert.True(File.Exists(Path.ChangeExtension(dbfPath, "dbt")));
        Assert.False(File.Exists(Path.ChangeExtension(dbfPath, "fpt")));
    }

    [Fact]
    public void CreateTyped_DateTimeAttribute_InfersVisualFoxPro()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create<TypedDateTimeModel>(dbfPath);

        Assert.Equal(DbfVersion.VisualFoxPro, dbf.Version);
    }

    [Fact]
    public void CreateTyped_Int32AutoIncrementAttribute_InfersVisualFoxProWithAutoIncrement()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        using var dbf = Dbf.Create<TypedInt32AutoIncrementModel>(dbfPath);

        Assert.Equal(DbfVersion.VisualFoxProWithAutoIncrement, dbf.Version);
    }

    [Fact]
    public void CreateTyped_UnsupportedPropertyAttributeCombination_ThrowsArgumentException()
    {
        using var temp = new TempDirectory();
        var dbfPath = Path.Combine(temp.Path, "table.dbf");

        Assert.Throws<ArgumentException>(() => Dbf.Create<UnsupportedAttributeModel>(dbfPath));

        Assert.False(File.Exists(dbfPath));
    }

    private static DbfFieldDescriptor InvokeFromProperty<T>(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");

        return (DbfFieldDescriptor)(s_fromProperty.Invoke(null, [property, DbfVersion.Unspecified])
            ?? throw new InvalidOperationException("FromProperty returned null."));
    }

    private static string GetExpectedDbfName(string name) =>
        name.Length > DbfFieldName.Size ? name[..DbfFieldName.Size] : name;

    private sealed class AttributeModel
    {
        [DbfCharacter(20, Name = "ALIAS", Flags = DbfFieldFlags.Nullable)]
        public string Character { get; set; } = string.Empty;

        [DbfNumeric(8, 2)]
        public double Numeric { get; set; }

        [DbfFloat(9, 3)]
        public double Float { get; set; }

        [DbfBinary]
        public byte[] BinaryMemo { get; set; } = [];

        [DbfBinary(8)]
        public double BinaryDouble { get; set; }

        [DbfMemo]
        public string Memo { get; set; } = string.Empty;

        [DbfOle]
        public byte[] Ole { get; set; } = [];

        [DbfPicture]
        public string Picture { get; set; } = string.Empty;

        [DbfNullFlags(1)]
        public byte[] NullFlags { get; set; } = [];

        [DbfVariant(30)]
        public string Variant { get; set; } = string.Empty;

        [DbfAutoIncrement]
        public int AutoIncrement { get; set; }

        [DbfBlob]
        public byte[] Blob { get; set; } = [];

        [DbfCurrency]
        public decimal Currency { get; set; }

        [DbfDate]
        public DateOnly Date { get; set; }

        [DbfDateTime]
        public DateTime DateTime { get; set; }

        [DbfDouble]
        public double Double { get; set; }

        [DbfInt32]
        public int Int32 { get; set; }

        [DbfInt32(AutoIncrement = true)]
        public int Int32AutoIncrement { get; set; }

        [DbfLogical]
        public bool Logical { get; set; }

        [DbfTimestamp]
        public DateTime Timestamp { get; set; }
    }

    private sealed class IgnoredPropertyModel
    {
        public string Name { get; set; } = string.Empty;

        [DbfIgnore]
        public string Ignored { get; set; } = "default";
    }

    private sealed class ConflictingIgnoredModel
    {
        [DbfIgnore]
        [DbfCharacter(10)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MultipleFieldAttributesModel
    {
        [DbfCharacter(10)]
        [DbfMemo]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class TypedCharacterModel
    {
        [DbfCharacter(20)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TypedMemoModel
    {
        [DbfMemo]
        public string Notes { get; set; } = string.Empty;
    }

    private sealed class TypedDateTimeModel
    {
        [DbfDateTime]
        public DateTime Updated { get; set; }
    }

    private sealed class TypedInt32AutoIncrementModel
    {
        [DbfInt32(AutoIncrement = true)]
        public int Id { get; set; }
    }

    private sealed class UnsupportedAttributeModel
    {
        [DbfMemo]
        public int Notes { get; set; }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory(nameof(DbfFieldAttributeTests)).FullName;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
