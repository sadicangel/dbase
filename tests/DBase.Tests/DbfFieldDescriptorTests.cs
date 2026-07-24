using System.Runtime.InteropServices;

namespace DBase.Tests;

public sealed class DbfFieldDescriptorTests
{
    public static TheoryData<DbfFieldName> InvalidFieldNames =>
    [
        default(DbfFieldName),
        new DbfFieldName(new byte[DbfFieldName.Size]),
        new DbfFieldName(new byte[] { (byte)'A', 0x80 })
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
