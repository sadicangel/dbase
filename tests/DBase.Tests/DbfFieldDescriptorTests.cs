using System.Runtime.InteropServices;

namespace DBase.Tests;

public sealed class DbfFieldDescriptorTests
{
    public static TheoryData<DbfFieldName> InvalidFieldNames =>
    [
        default(DbfFieldName),
        new DbfFieldName(new byte[DbfFieldName.Size]),
        new DbfFieldName([(byte)'A', 0x80])
    ];

    [Fact]
    public void Numeric_ReturnsNumericType()
    {
        var descriptor = DbfFieldDescriptor.Numeric("AMOUNT", 8, 2);

        Assert.Equal(DbfFieldType.Numeric, descriptor.Type);
    }

    [Fact]
    public void Float_ReturnsFloatType()
    {
        var descriptor = DbfFieldDescriptor.Float("AMOUNT", 8, 2);

        Assert.Equal(DbfFieldType.Float, descriptor.Type);
    }

    [Fact]
    public void Double_ReturnsDoubleType()
    {
        var descriptor = DbfFieldDescriptor.Double("AMOUNT");

        Assert.Equal(DbfFieldType.Double, descriptor.Type);
        Assert.Equal((byte)8, descriptor.Length);
    }

    [Fact]
    public void Int32_ReturnsInt32Type()
    {
        var descriptor = DbfFieldDescriptor.Int32("COUNT");

        Assert.Equal(DbfFieldType.Int32, descriptor.Type);
        Assert.Equal((byte)4, descriptor.Length);
    }

    [Fact]
    public void Timestamp_ReturnsTimestampType()
    {
        var descriptor = DbfFieldDescriptor.Timestamp("STAMP");

        Assert.Equal(DbfFieldType.Timestamp, descriptor.Type);
        Assert.Equal((byte)8, descriptor.Length);
    }

    [Fact]
    public void Variant_ReturnsVariantType()
    {
        var descriptor = DbfFieldDescriptor.Variant("VALUE", 20);

        Assert.Equal(DbfFieldType.Variant, descriptor.Type);
        Assert.Equal((byte)20, descriptor.Length);
    }

    [Fact]
    public void FieldFlags_FormatValues_MatchDescriptorBits()
    {
        Assert.Equal(0x00, (byte)DbfFieldFlags.None);
        Assert.Equal(0x02, (byte)DbfFieldFlags.Nullable);
    }

    [Theory]
    [MemberData(nameof(InvalidFieldNames))]
    public void Character_InvalidFieldName_ThrowsArgumentException(DbfFieldName name)
    {
        var exception = Assert.Throws<ArgumentException>(() => DbfFieldDescriptor.Character(name, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Character_NullFieldName_ThrowsArgumentException()
    {
        string? name = null;

        var exception = Assert.Throws<ArgumentException>(() => DbfFieldDescriptor.Character(name, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Character_NonAsciiFieldName_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => DbfFieldDescriptor.Character("CAF\u00C9", 4));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void AutoIncrement_UsesFourByteDescriptorLayout()
    {
        var descriptor = DbfFieldDescriptor.AutoIncrement("ID");

        Assert.Equal(DbfFieldType.AutoIncrement, descriptor.Type);
        Assert.Equal((byte)4, descriptor.Length);
        Assert.Equal(DbfFieldFlags.AutoIncrement, descriptor.Flags);

        Span<byte> raw = stackalloc byte[DbfFieldDescriptor.Size];
        MemoryMarshal.Write(raw, in descriptor);

        Assert.Equal((byte)DbfFieldType.AutoIncrement, raw[11]);
        Assert.Equal((byte)4, raw[16]);
        Assert.Equal((byte)DbfFieldFlags.AutoIncrement, raw[18]);
    }
}
