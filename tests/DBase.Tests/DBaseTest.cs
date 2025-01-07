using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace DBase.Tests;

public abstract class DBaseTest
{
    private static readonly Lazy<CsvConfiguration> s_csvConfiguration = new(() =>
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat = (DateTimeFormatInfo)CultureInfo.CreateSpecificCulture("en-GB").DateTimeFormat.Clone();

        return new CsvConfiguration(culture)
        {
            TrimOptions = TrimOptions.None
        };
    });

    private string DbfPath => Path.Combine(GetType().Name, $"{GetType().Name}.dbf");

    [Fact]
    public Task VerifyHeader()
    {
        using var dbf = Dbf.Open(DbfPath);
        return Verifier.Verify(target: dbf.Header);
    }

    [Fact]
    public Task VerifyReader()
    {
        using var dbf = Dbf.Open(DbfPath);
        using var output = new MemoryStream();
        using (var writer = new CsvWriter(new StreamWriter(output), s_csvConfiguration.Value))
        {

            foreach (var descriptor in dbf.Descriptors)
                writer.WriteField(descriptor.Name.ToString());
            writer.NextRecord();

            foreach (var record in dbf)
            {
                foreach (var field in record)
                    writer.WriteField(field.ToString(s_csvConfiguration.Value.CultureInfo).ReplaceLineEndings("    "));
                writer.NextRecord();
            }
        }

        var target = Encoding.UTF8.GetString(output.ToArray());
        return Verifier.Verify(target: target, extension: "csv");
    }

    [Fact]
    public Task VerifyMemo()
    {
        using var dbf = Dbf.Open(DbfPath);

        var memo = dbf.Memo;

        if (memo is not null)
        {
            using var writer = new StringWriter();

            var index = Math.Max(1, DbtHeader.HeaderLengthInDisk / memo.BlockLength);
            foreach (var record in memo)
            {
                writer.WriteLine($"{index} ({record.Type}) ({record.Length}b): ");
                writer.WriteLine(dbf.Encoding.GetString(record.Span));
                writer.WriteLine();
                index += 1 + record.Length / memo.BlockLength;
            }

            var target = writer.ToString();

            return Verifier.Verify(target: target);
        }

        Assert.Skip($"'{Path.GetFileName(DbfPath)}' does not have a corresponding memo file.");
        return Task.CompletedTask;
    }
}
