namespace DBase.Tests;

public sealed class DbfFieldTests
{
    [Fact]
    public void IsNull_NullField_ReturnsTrue()
    {
        DbfField field = (string?)null;

        Assert.True(field.IsNull);
        Assert.False(field.IsType<string>());
    }

    [Fact]
    public void IsType_ExactStoredType_ReturnsTrue()
    {
        DbfField field = 42;

        Assert.False(field.IsNull);
        Assert.True(field.IsType<int>());
        Assert.False(field.IsType<long>());
    }
}
