using System.Text;
using CsvHelper;

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
}
