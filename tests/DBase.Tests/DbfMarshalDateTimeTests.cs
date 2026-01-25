using DBase.Interop;

namespace DBase.Tests;

public class DbfMarshalDateTimeTests
{
    public static TheoryData<DateTime> TestDates =>
    [
        new DateTime(2025, 3, 2, 15, 30, 45, 123),
        new DateTime(2000, 1, 1, 0, 0, 0, 0), // Midnight
        new DateTime(1900, 1, 1, 12, 0, 0, 500), // Noon + 500ms
        //DateTime.MinValue,
        //DateTime.MaxValue,
    ];

    [Theory]
    [MemberData(nameof(TestDates))]
    public void Roundtrip_ShouldPreserveDateTime(DateTime originalDate)
    {
        var buffer = new byte[8];

        DbfMarshal.WriteDateTime(buffer, originalDate);
        var decodedDate = DbfMarshal.ReadDateTime(buffer);

        Assert.NotNull(decodedDate);
        Assert.Equal(originalDate, decodedDate.Value);
    }

    [Fact]
    public void DefaultDateTime_ShouldReturnNull()
    {
        var buffer = new byte[8];

        DbfMarshal.WriteDateTime(buffer, null);
        var decodedDate = DbfMarshal.ReadDateTime(buffer);

        Assert.Null(decodedDate);
    }

    [Fact]
    public void ZeroedBuffer_ShouldReturnNull()
    {
        var buffer = new byte[8]; // All zeroes
        var decodedDate = DbfMarshal.ReadDateTime(buffer);
        Assert.Null(decodedDate);
    }
}
