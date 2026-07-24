using System.Text;

namespace DBase.Tests;

public sealed class DbcBacklinkTests
{
    [Fact]
    public void Properties_DecodeFirstTwoNullTerminatedStrings()
    {
        var content = new byte[DbcBacklink.Size];
        Encoding.UTF8.GetBytes("data.dbc\0CUSTOMERS\0").CopyTo(content, 0);

        var backlink = new DbcBacklink(content);

        Assert.Equal("data.dbc", backlink.DbcPath);
        Assert.Equal("CUSTOMERS", backlink.TableName);
    }
}
