﻿using System.Globalization;
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
        using var stream = File.OpenRead(DbfPath);
        using var reader = new DbfReader(stream);
        return Verifier.Verify(target: reader.Header);
    }

    [Fact]
    public Task Verify()
    {
        using var stream = File.OpenRead(DbfPath);
        using var reader = new DbfReader(stream);
        using var output = new MemoryStream();
        using (var writer = new CsvWriter(new StreamWriter(output), s_csvConfiguration.Value))
        {

            foreach (var descriptor in reader.Descriptors)
                writer.WriteField(descriptor.Name.ToString());
            writer.NextRecord();

            foreach (var record in reader.Read())
            {
                foreach (var field in record)
                    writer.WriteField(field.ToString(s_csvConfiguration.Value.CultureInfo));
                writer.NextRecord();
            }
        }

        var target = Encoding.UTF8.GetString(output.ToArray());
        return Verifier.Verify(target: target, extension: "csv");
    }
}
