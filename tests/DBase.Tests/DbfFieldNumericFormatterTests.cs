using System.Text;
using DBase.Serialization.Fields;

namespace DBase.Tests;

public sealed class DbfFieldNumericFormatterTests
{
    [Fact]
    public void WriteRaw_Integer_RightAlignsWithinFieldWidth()
    {
        var buffer = new byte[6];

        DbfFieldNumericFormatter.WriteRaw(buffer, 123L, Encoding.ASCII);

        Assert.Equal("   123", Encoding.ASCII.GetString(buffer));
    }

    [Fact]
    public void WriteRaw_Decimal_RightAlignsWithinFieldWidth()
    {
        var buffer = new byte[6];

        DbfFieldNumericFormatter.WriteRaw(buffer, 12.3D, 2, Encoding.ASCII, '.');

        Assert.Equal(" 12.30", Encoding.ASCII.GetString(buffer));
    }

    [Fact]
    public void WriteRaw_IntegerTooWide_ThrowsOverflowException()
    {
        var buffer = new byte[3];

        var exception = Assert.Throws<OverflowException>(() => DbfFieldNumericFormatter.WriteRaw(buffer, 1234L, Encoding.ASCII));

        Assert.Contains("field length is 3", exception.Message);
    }

    [Fact]
    public void WriteRaw_DecimalTooWide_ThrowsOverflowException()
    {
        var buffer = new byte[5];

        var exception = Assert.Throws<OverflowException>(() => DbfFieldNumericFormatter.WriteRaw(buffer, -12.34D, 2, Encoding.ASCII, '.'));

        Assert.Contains("field length is 5", exception.Message);
    }
}
