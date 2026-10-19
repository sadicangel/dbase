using System.Collections.Immutable;

namespace DBase.Tests;

public sealed class DbfRecordTests
{
    [Fact]
    public void Constructor_DescriptorsAndFields_RetainsSchemaAndValues()
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 10));
        var record = new DbfRecord(descriptors, ["Ada"]);

        Assert.True(descriptors == record.Descriptors);
        Assert.Single(record);
        Assert.Equal("Ada", record[0].GetValue<string>());
        Assert.False(record.IsDeleted);
    }

    [Fact]
    public void Constructor_DefaultDescriptors_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new DbfRecord(default, ["Ada"]));

        Assert.Equal("descriptors", exception.ParamName);
    }

    [Fact]
    public void Constructor_DefaultFields_ThrowsArgumentException()
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 10));

        var exception = Assert.Throws<ArgumentException>(() => new DbfRecord(descriptors, default));

        Assert.Equal("fields", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Constructor_FieldCountMismatch_ThrowsArgumentException(int fieldCount)
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 10));
        var fields = ImmutableArray.CreateRange(new DbfField[fieldCount]);

        var exception = Assert.Throws<ArgumentException>(() => new DbfRecord(descriptors, fields));

        Assert.Equal("fields", exception.ParamName);
    }

    [Fact]
    public void Constructor_EmptySchemaAndFields_CreatesInitializedEmptyRecord()
    {
        var record = new DbfRecord([], []);

        Assert.False(record.Descriptors.IsDefault);
        Assert.Empty(record);
    }

    [Fact]
    public void Add_ExpandedFields_RoundTripsWithSharedDescriptors()
    {
        using var dbf = Dbf.Create(new MemoryStream(),
            [DbfFieldDescriptor.Character("NAME", 10), DbfFieldDescriptor.Numeric("AGE", 3)]);

        dbf.Add("Ada", 36L);
        dbf.Add("Grace", 85L);

        var records = dbf.EnumerateRecords().ToArray();
        Assert.Equal(2, dbf.RecordCount);
        Assert.All(records, record => Assert.True(dbf.Descriptors == record.Descriptors));
        Assert.Equal("Ada", records[0][0].GetValue<string>());
        Assert.Equal(36L, records[0][1].GetValue<long>());
        Assert.Equal("Grace", records[1][0].GetValue<string>());
        Assert.Equal(85L, records[1][1].GetValue<long>());
        Assert.All(records, record => Assert.False(record.IsDeleted));
    }

    [Fact]
    public void Add_SingleFieldCollectionExpression_RoundTrips()
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Character("NAME", 10)]);

        dbf.Add(["Ada"]);

        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
    }

    [Fact]
    public void Add_SingleString_PrefersFieldOverload()
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Character("NAME", 10)]);

        dbf.Add("Ada");

        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
    }

    [Fact]
    public void Add_SingleNumericValue_PrefersFieldOverload()
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Numeric("AGE", 3)]);

        dbf.Add(36L);

        Assert.Equal(36L, dbf[0][0].GetValue<long>());
    }

    [Fact]
    public void Add_SingleDbfField_PrefersFieldOverload()
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Character("NAME", 10)]);
        DbfField field = "Ada";

        dbf.Add(field);

        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
    }

    [Fact]
    public void Add_FieldArray_PrefersFieldOverload()
    {
        using var dbf = Dbf.Create(new MemoryStream(),
            [DbfFieldDescriptor.Character("NAME", 10), DbfFieldDescriptor.Numeric("AGE", 3)]);
        DbfField[] fields = ["Ada", 36L];

        dbf.Add(fields);

        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
        Assert.Equal(36L, dbf[0][1].GetValue<long>());
    }

    [Fact]
    public void Add_ReadOnlySpan_WritesValuesWithoutRetainingCallerStorage()
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Character("NAME", 10)]);
        DbfField[] fields = ["Ada"];

        dbf.Add((ReadOnlySpan<DbfField>)fields);
        fields[0] = "Grace";

        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Add_FieldCountMismatch_ThrowsWithoutAppending(int fieldCount)
    {
        using var bytes = new MemoryStream();
        using var dbf = Dbf.Create(bytes, [DbfFieldDescriptor.Character("NAME", 10)]);
        dbf.Add(["Ada"]);
        var length = bytes.Length;
        var position = bytes.Position;
        var fields = new DbfField[fieldCount];

        var exception = Assert.Throws<ArgumentException>(() => dbf.Add((ReadOnlySpan<DbfField>)fields));

        Assert.Equal("fields", exception.ParamName);
        Assert.Equal(1, dbf.RecordCount);
        Assert.Equal(length, bytes.Length);
        Assert.Equal(position, bytes.Position);
        Assert.Equal("Ada", dbf[0][0].GetValue<string>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Add_DefaultRecord_ThrowsWithoutAppending(bool generic)
    {
        using var bytes = new MemoryStream();
        using var dbf = Dbf.Create(bytes, [DbfFieldDescriptor.Character("NAME", 10)]);
        var length = bytes.Length;

        var exception = Assert.Throws<ArgumentException>(() =>
        {
            if (generic) dbf.Add<DbfRecord>(default);
            else dbf.Add(default(DbfRecord));
        });

        Assert.Equal("record", exception.ParamName);
        Assert.Equal(0, dbf.RecordCount);
        Assert.Equal(length, bytes.Length);
    }

    [Fact]
    public void Add_RecordWithDifferentSchema_UsesDestinationSchema()
    {
        var sourceDescriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("SOURCE", 10));
        var source = new DbfRecord(DbfRecordStatus.Deleted, sourceDescriptors, ["Ada"]);
        using var bytes = new MemoryStream();
        using var dbf = Dbf.Create(bytes, [DbfFieldDescriptor.Character("TARGET", 5)]);

        dbf.Add(source);
        var copied = dbf[0];

        Assert.True(dbf.Descriptors == copied.Descriptors);
        Assert.True(sourceDescriptors == source.Descriptors);
        Assert.Equal("TARGET", copied.Descriptors[0].Name.ToString());
        Assert.Equal(source, copied);
        Assert.Equal(source.GetHashCode(), copied.GetHashCode());
        Assert.True(copied.IsDeleted);
        Assert.Equal("*Ada  "u8.ToArray(), bytes.ToArray().AsSpan(dbf.HeaderLength, dbf.RecordLength).ToArray());
    }

    [Fact]
    public void Add_RecordWithDifferentFieldCount_ThrowsWithoutAppending()
    {
        var source = new DbfRecord([DbfFieldDescriptor.Character("NAME", 10)], ["Ada"]);
        using var bytes = new MemoryStream();
        using var dbf = Dbf.Create(bytes,
            [DbfFieldDescriptor.Character("NAME", 10), DbfFieldDescriptor.Numeric("AGE", 3)]);
        var length = bytes.Length;

        Assert.Throws<ArgumentException>(() => dbf.Add(source));

        Assert.Equal(0, dbf.RecordCount);
        Assert.Equal(length, bytes.Length);
    }

    [Fact]
    public void GetRecord_DisposedTable_RetainsSchemaAndValues()
    {
        DbfRecord record;
        ImmutableArray<DbfFieldDescriptor> descriptors;
        using (var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Character("NAME", 10)]))
        {
            dbf.Add(["Ada"]);
            record = dbf.GetRecord<DbfRecord>(0);
            descriptors = dbf.Descriptors;
        }

        Assert.True(descriptors == record.Descriptors);
        Assert.Equal("NAME", record.Descriptors[0].Name.ToString());
        Assert.Equal("Ada", record[0].GetValue<string>());
    }
}
