using System.Text;
using CsvHelper;
using Xunit.Sdk;

namespace DBase.Tests;

public abstract class DBaseTest<T> : DBaseTest
{
    [Fact]
    public Task VerifyReaderTyped()
    {
        using var dbf = Dbf.Open(DbfPath);
        var target = WriteCsvTyped(dbf);
        return Verify(target: target, extension: "csv");
    }

    [Fact]
    public Task VerifyWriterTyped()
    {
        using var dbf = ReadThenWrite(DbfPath);
        var target = WriteCsvTyped(dbf);
        return Verify(target: target, extension: "csv");

        static Dbf ReadThenWrite(string path)
        {
            using var old = Dbf.Open(path);
            var dbf = Dbf.Create(
                new MemoryStream(),
                old.Descriptors,
                old.Memo is not null ? new MemoryStream() : null,
                old.Version,
                old.Language);

            foreach (var record in old.EnumerateRecords<T>())
                dbf.Add(record);

            return dbf;
        }
    }

    private static string WriteCsvTyped(Dbf dbf)
    {
        using var output = new MemoryStream();
        using (var writer = new CsvWriter(new StreamWriter(output), CsvConfiguration.Value))
        {
            writer.Context.TypeConverterCache.AddConverter(NoLineBreaksStringConverter.Instance);
            foreach (var descriptor in dbf.Descriptors)
                writer.WriteField(descriptor.Name.ToString());
            writer.NextRecord();

            foreach (var record in dbf.EnumerateRecords<T>())
            {
                writer.WriteRecord(record);
                writer.NextRecord();
            }
        }

        return Encoding.UTF8.GetString(output.ToArray());
    }

    [Fact]
    public void RoundtripHeader()
    {
        using var source = Dbf.Open(DbfPath);
        using var oldDbf = new MemoryStream();
        using var oldMemo = new MemoryStream();
        source.WriteTo(oldDbf, oldMemo);

        var target = Dbf.Create(
            new MemoryStream(),
            source.Descriptors,
            source.Memo is not null ? new MemoryStream() : null,
            source.Version,
            source.Language);

        foreach (var record in source.EnumerateRecords<T>())
            target.Add(record);

        using var newDbf = new MemoryStream();
        using var newMemo = new MemoryStream();
        target.WriteTo(newDbf, newMemo);

        var expected = oldDbf.ToArray().AsSpan();
        var actual = newDbf.ToArray().AsSpan();

        var version = source.Version;
        if (version is DbfVersion.DBase02)
        {
            AssertBytes(expected, actual, 0..1, "version");
            AssertBytes(expected, actual, 1..3, "record count");
            // AssertBytes(expected, actual, 3..6, "last update");
            AssertBytes(expected, actual, 6..8, "record length");
        }
        else
        {
            AssertBytes(expected, actual, 0..1, "version");
            // AssertBytes(expected, actual, 1..4, "last update");
            AssertBytes(expected, actual, 4..8, "record count");
            AssertBytes(expected, actual, 8..10, "header length");
            AssertBytes(expected, actual, 10..12, "record length");
            AssertBytes(expected, actual, 28..29, "flags", GetTableFlagsComparer());
            AssertBytes(expected, actual, 29..30, "language");
        }

        return;

        static EqualityComparer<byte[]> GetTableFlagsComparer() => EqualityComparer<byte[]>.Create(
            (x, y) =>
            {
                if (x?.Length != 1 || y?.Length != 1) return false;
                var a = x[0] & ~(int)DbfTableFlags.HasStructuralCdx;
                var b = y[0] & ~(int)DbfTableFlags.HasStructuralCdx;
                return a == b;
            },
            obj =>
            {
                if (obj.Length != 1) return 0;
                return obj[0] & ~(int)DbfTableFlags.HasStructuralCdx;
            });
    }

    private static void AssertBytes(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, Range range, string label, IEqualityComparer<byte[]>? comparer = null)
    {
        try
        {
            if (comparer is not null)
            {
                Assert.Equal(expected[range].ToArray(), actual[range].ToArray(), comparer);
            }
            else
            {
                Assert.Equal(expected[range], actual[range]);
            }
        }
        catch (XunitException ex)
        {
            throw new XunitException($"Value '{label}' mismatch ({range.Start.Value}..{range.End.Value}):\n{ex.Message}");
        }
    }

    private static int AssertEqualDescriptors(DbfVersion version, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        if (version is DbfVersion.DBase02)
        {
            Assert.Equal(expected[0..1], actual[0..1]); // Version
            Assert.Equal(expected[1..3], actual[1..3]); // Record count
            if (expected[3..6] is not [0, 0, 0])
                Assert.Equal(expected[3..6], actual[3..6]); // Last update
            Assert.Equal(expected[6..8], actual[6..8]); // Record length

            return 8;
        }
        else
        {
            return 32;
        }
    }
}
