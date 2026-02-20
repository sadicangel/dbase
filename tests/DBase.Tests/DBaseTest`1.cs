using System.Buffers.Binary;
using System.Text;
using CsvHelper;
using DBase.Interop;
using DBase.Tests.Helpers;

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

    private readonly ref struct ReadWriteReturnResult(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        public ReadOnlySpan<byte> Expected { get; } = expected;
        public ReadOnlySpan<byte> Actual { get; } = actual;

        public void Deconstruct(out ReadOnlySpan<byte> expected, out ReadOnlySpan<byte> actual)
        {
            expected = Expected;
            actual = Actual;
        }
    }

    private static ReadWriteReturnResult ReadWriteReturnTyped(Dbf source)
    {
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

        return new ReadWriteReturnResult(expected, actual);
    }

    [Fact]
    public void RoundtripHeader()
    {
        using var source = Dbf.Open(DbfPath);
        var (expected, actual) = ReadWriteReturnTyped(source);

        if (source.Version is DbfVersion.DBase02)
        {
            AssertBytes(expected, actual, 0..1, "version", static span => (DbfVersion)span[0]);
            AssertBytes(expected, actual, 1..3, "record count", static span => BinaryPrimitives.ReadUInt16LittleEndian(span));
            //AssertBytes(expected, actual, 3..6, "last update");
            AssertBytes(expected, actual, 6..8, "record length", static span => BinaryPrimitives.ReadUInt16LittleEndian(span));
        }
        else
        {
            AssertBytes(expected, actual, 0..1, "version", static span => (DbfVersion)span[0]);
            //AssertBytes(expected, actual, 1..4, "last update");
            AssertBytes(expected, actual, 4..8, "record count", static span => BinaryPrimitives.ReadUInt32LittleEndian(span));
            AssertBytes(expected, actual, 8..10, "header length", static span => BinaryPrimitives.ReadUInt16LittleEndian(span));
            AssertBytes(expected, actual, 10..12, "record length", static span => BinaryPrimitives.ReadUInt16LittleEndian(span));
            AssertBytes(expected, actual, 28..29, "flags", static span => (DbfTableFlags)span[0], GetTableFlagsComparer());
            AssertBytes(expected, actual, 29..30, "language", static span => (DbfLanguage)span[0]);
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

    [Fact]
    public void RoundtripDescriptors()
    {
        using var source = Dbf.Open(DbfPath);
        var (expected, actual) = ReadWriteReturnTyped(source);

        if (source.Version is DbfVersion.DBase02)
        {
            expected = expected[DbfHeader02.Size..];
            actual = actual[DbfHeader02.Size..];
            while (expected[0] != 0x0D)
            {
                AssertBytes(expected, actual, 0..11, "name", static span => Encoding.UTF8.GetString(span));
                AssertBytes(expected, actual, 11..12, "data type", static span => (DbfFieldType)span[0]);
                AssertBytes(expected, actual, 12..13, "length");
                //AssertBytes(expected, actual, 13..15, "offset", static span => BinaryPrimitives.ReadUInt16LittleEndian(span));
                AssertBytes(expected, actual, 15..16, "decimal");

                expected = expected[DbfFieldDescriptor02.Size..];
                actual = actual[DbfFieldDescriptor02.Size..];
            }
        }
        else
        {
            expected = expected[DbfHeader.Size..];
            actual = actual[DbfHeader.Size..];
            while (expected[0] != 0x0D)
            {
                AssertBytes(expected, actual, 0..11, "name", static span => Encoding.UTF8.GetString(span));
                AssertBytes(expected, actual, 11..12, "data type", static span => (DbfFieldType)span[0]);
                //AssertBytes(expected, actual, 12..16, "offset", static span => BinaryPrimitives.ReadUInt32LittleEndian(span));
                AssertBytes(expected, actual, 16..17, "length");
                AssertBytes(expected, actual, 17..18, "decimal");

                expected = expected[DbfFieldDescriptor.Size..];
                actual = actual[DbfFieldDescriptor.Size..];
            }
        }
    }

    [Fact]
    public void RoundtripRecords()
    {
        using var source = Dbf.Open(DbfPath);
        var (expected, actual) = ReadWriteReturnTyped(source);

        expected = expected[source.HeaderLength..];
        actual = actual[source.HeaderLength..];

        var recordNumber = 0;
        var encoding = source.Encoding;
        while (recordNumber < source.RecordCount)
        {
            //AssertBytes(expected, actual, 0..1, $"record: {recordNumber}: 'status'", static span => (DbfRecordStatus)span[0]);
            foreach (var (descriptor, comparer) in source.Descriptors.Zip(source.Descriptors.Select(d => d.GetRawEqualityComparer(encoding))))
            {
                AssertBytes(
                    expected,
                    actual,
                    descriptor.Range,
                    $"record: {recordNumber}: '{descriptor.Name}'",
                    comparer: comparer);
            }

            expected = expected[source.RecordLength..];
            actual = actual[source.RecordLength..];
            recordNumber++;
        }
    }
}
