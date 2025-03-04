using DBase.Interop;

namespace DBase.Tests;
public class DbfMarshal_DateTime_Tests
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

        DbfMarshal.WriteDateTime(buffer, default);
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

    [Fact]
    public void QEWR()
    {
        using var dbf = Dbf.Create(
            new MemoryStream(),
            [
                DbfFieldDescriptor.AutoIncrement("Id"),
                DbfFieldDescriptor.Text("Name", 20),
            ]);

        dbf.Add(new Record(1, "Alice"));
        dbf.Add(new Record(2, "Bob"));

    }

    private record class Record(int Id, string Name);
}
