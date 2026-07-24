using DBase.Serialization.Fields;

namespace DBase.Tests;

public sealed class DbfFieldAutoIncrementFormatterTests
{
    [Fact]
    public void WriteRaw_WritesFourByteLittleEndianValue()
    {
        var buffer = new byte[4];

        DbfFieldAutoIncrementFormatter.WriteRaw(buffer, 0x01020304);

        Assert.Equal([0x04, 0x03, 0x02, 0x01], buffer);
        Assert.Equal(0x01020304, DbfFieldAutoIncrementFormatter.ReadRaw(buffer));
    }
}
