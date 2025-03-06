using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DBase.Tests;

public abstract class DBaseTest<T>
{
    protected static readonly Lazy<CsvConfiguration> CsvConfiguration = new(() =>
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat = (DateTimeFormatInfo)CultureInfo.CreateSpecificCulture("en-GB").DateTimeFormat.Clone();

        return new CsvConfiguration(culture)
        {
            TrimOptions = TrimOptions.None,
        };
    });

    protected string DbfPath => Path.Combine(GetType().Name, $"{GetType().Name}.dbf");

    [Fact]
    public Task VerifyHeader()
    {
        using var dbf = Dbf.Open(DbfPath);
        return Verifier.Verify(target: new
        {
            dbf.Version,
            dbf.Language,
            LastUpdate = dbf.LastUpdate.ToString("yyyy-MM-dd"),
            dbf.HeaderLength,
            dbf.RecordLength,
            dbf.RecordCount,
            Descriptors = dbf.Descriptors.Select(d => $"| {d.Name,-10} | {(char)d.Type} | {d.Length,3} | {d.Decimal,2} |"),
        });
    }

    [Fact]
    public Task VerifyReader()
    {
        using var dbf = Dbf.Open(DbfPath);
        var target = WriteCsv(dbf);
        return Verifier.Verify(target: target, extension: "csv");
    }

    [Fact]
    public Task VerifyReaderTyped()
    {
        using var dbf = Dbf.Open(DbfPath);
        var target = WriteCsvTyped(dbf);
        return Verifier.Verify(target: target, extension: "csv");
    }

    [Fact]
    public Task VerifyWriter()
    {
        using var dbf = ReadThenWrite(DbfPath);
        var target = WriteCsv(dbf);
        return Verifier.Verify(target: target, extension: "csv");

        static Dbf ReadThenWrite(string path)
        {
            using var old = Dbf.Open(path);
            var dbf = Dbf.Create(
                new MemoryStream(),
                old.Descriptors,
                old.Memo is not null ? new MemoryStream() : null,
                old.Version,
                old.Language);

            foreach (var record in old.EnumerateRecords())
                dbf.Add(record);

            return dbf;
        }
    }

    [Fact]
    public Task VerifyWriterTyped()
    {
        using var dbf = ReadThenWrite(DbfPath);
        var target = WriteCsvTyped(dbf);
        return Verifier.Verify(target: target, extension: "csv");

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

    [Fact]
    public Task VerifyMemo()
    {
        using var dbf = Dbf.Open(DbfPath);

        var memo = dbf.Memo;

        if (memo is not null)
        {
            using var writer = new StringWriter();

            var index = memo.FirstIndex;
            foreach (var record in memo)
            {
                writer.WriteLine($"{index} ({record.Type}) ({record.Length}b): ");
                var content = dbf.Encoding.GetString(record.Span);
                writer.WriteLine(content);
                writer.WriteLine();
                index += memo.GetBlockCount(record.Span);
            }

            var target = writer.ToString();

            return Verifier.Verify(target: target);
        }

        Assert.Skip($"'{Path.GetFileName(DbfPath)}' does not have a corresponding memo file.");
        return Task.CompletedTask;
    }

    private static string WriteCsv(Dbf dbf)
    {
        using var output = new MemoryStream();
        using (var writer = new CsvWriter(new StreamWriter(output), CsvConfiguration.Value))
        {
            writer.Context.TypeConverterCache.AddConverter(NoLineBreaksStringConverter.Instance);
            foreach (var descriptor in dbf.Descriptors)
                writer.WriteField(descriptor.Name.ToString());
            writer.NextRecord();

            foreach (var record in dbf.EnumerateRecords())
            {
                foreach (var field in record)
                    writer.WriteField(field.ToString(CsvConfiguration.Value.CultureInfo), NoLineBreaksStringConverter.Instance);
                writer.NextRecord();
            }
        }

        return Encoding.UTF8.GetString(output.ToArray());
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

    private sealed class NoLineBreaksStringConverter : TypeConverter<string>
    {
        private static readonly StringConverter s_stringConverter = new();

        public static readonly NoLineBreaksStringConverter Instance = new();

        private NoLineBreaksStringConverter() { }

        public override string? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
            (string?)s_stringConverter.ConvertFromString(text, row, memberMapData);

        public override string? ConvertToString(string? value, IWriterRow row, MemberMapData memberMapData) =>
            s_stringConverter.ConvertToString(value, row, memberMapData)?.ReplaceLineEndings("    ");
    }
}
