using System.Collections.Immutable;

using System.Runtime.CompilerServices;
using System.Text;
using DBase.Serialization;

namespace DBase.Tests;

public sealed class DbfRecordSerializerTests
{
    [Fact]
    public void UntypedRoundtrip_DeletedRecord_PreservesDeletedStatus()
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("Name", 10));

        using var source = Dbf.Create(new MemoryStream(), descriptors);
        source.Add(new DbfRecord(DbfRecordStatus.Deleted, (DbfField)"gone"));

        using var target = Dbf.Create(
            new MemoryStream(),
            source.Descriptors,
            options: new DbfCreateOptions
            {
                Version = source.Version,
                Language = source.Language,
            });
        foreach (var record in source.EnumerateRecords())
        {
            target.Add(record);
        }

        var copied = target.GetRecord(0);
        Assert.True(copied.IsDeleted);

        using var bytes = new MemoryStream();
        target.WriteTo(bytes, null);
        Assert.Equal((byte)DbfRecordStatus.Deleted, bytes.ToArray()[target.HeaderLength]);
    }

    [Fact]
    public void TypedRoundtrip_ParameterlessMutableClass_MaterializesProperties()
    {
        var descriptors = ImmutableArray.Create(
            DbfFieldDescriptor.Character("Name", 10),
            DbfFieldDescriptor.Numeric("Age"));

        using var source = Dbf.Create(new MemoryStream(), descriptors);
        source.Add(new MutableCustomer
        {
            Name = "Ada",
            Age = 42
        });

        var actual = source.GetRecord<MutableCustomer>(0);

        Assert.Equal("Ada", actual.Name);
        Assert.Equal(42, actual.Age);
    }

    [Fact]
    public void CreateTyped_NullableCurrencyWithValue_RoundTrips()
    {
        var dbfPath = GetTempDbfPath();

        try
        {
            using (var source = Dbf.Create<NullableCurrencyRecord>(dbfPath, new DbfCreateOptions { Version = DbfVersion.VisualFoxPro }))
            {
                source.Add(new NullableCurrencyRecord { Amount = 12.34m });
            }

            using var reopened = Dbf.Open(dbfPath);
            var actual = reopened.GetRecord<NullableCurrencyRecord>(0);

            Assert.Equal(12.34m, actual.Amount);
        }
        finally
        {
            DeleteIfExists(dbfPath);
        }
    }

    [Fact]
    public void CreateTyped_NullableCurrencyWithNull_ThrowsInvalidOperationException()
    {
        var dbfPath = GetTempDbfPath();

        try
        {
            using var source = Dbf.Create<NullableCurrencyRecord>(dbfPath, new DbfCreateOptions { Version = DbfVersion.VisualFoxPro });

            var exception = Assert.Throws<InvalidOperationException>(
                () => source.Add(new NullableCurrencyRecord { Amount = null }));

            Assert.Contains("Currency fields cannot serialize null values", exception.Message);
        }
        finally
        {
            DeleteIfExists(dbfPath);
        }
    }

    [Fact]
    public void TypedRoundtrip_GeneratedBinaryLikeClass_RoundTripsRawBytes()
    {
        var fixedValue = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var payload = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var nullFlags = new byte[] { 0xA5 };
        var descriptors = ImmutableArray.Create(
            DbfFieldDescriptor.Binary("Fixed", 8),
            DbfFieldDescriptor.Blob("Payload"),
            DbfFieldDescriptor.NullFlags("_NullFlags", 1));

        using var source = Dbf.Create(
            new MemoryStream(),
            descriptors,
            new MemoryStream(),
            new DbfCreateOptions { Version = DbfVersion.VisualFoxPro });
        source.Add(new GeneratedBinaryLikeRecord
        {
            Fixed = fixedValue,
            Payload = payload,
            _NullFlags = nullFlags
        });

        using var dbfBytes = new MemoryStream();
        using var memoBytes = new MemoryStream();
        source.WriteTo(dbfBytes, memoBytes);
        dbfBytes.Position = 0;
        memoBytes.Position = 0;

        using var reopened = Dbf.Open(dbfBytes, memoBytes, null);
        var actual = reopened.GetRecord<GeneratedBinaryLikeRecord>(0);

        Assert.Equal(fixedValue, actual.Fixed);
        Assert.Equal(payload, actual.Payload);
        Assert.Equal(nullFlags, actual._NullFlags);
    }

    [Fact]
    public void UntypedSerializer_BinaryFields_RoundTripsWithoutBoxing()
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Int32("Id"),
            DbfFieldDescriptor.Double("Number"), DbfFieldDescriptor.Currency("Amount"),
            DbfFieldDescriptor.Logical("Active"));
        using var dbf = Dbf.Create(new MemoryStream(), descriptors,
            options: new DbfCreateOptions { Version = DbfVersion.VisualFoxPro });
        var serializer = new DbfRecordSerializer<DbfRecord>(dbf.Descriptors);
        var context = new DbfSerializationContext(Encoding.ASCII, null, '.');
        var record = new DbfRecord(DbfRecordStatus.Deleted, (DbfField)42, (DbfField)1.5D,
            (DbfField)12.3456M, (DbfField)true);
        var bytes = new byte[dbf.RecordLength];
        byte[] expected =
        [
            0x2A, // Deleted record marker.
            0x2A, 0x00, 0x00, 0x00, // Int32: 42.
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0x3F, // Double: 1.5.
            0x40, 0xE2, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, // Currency: 12.3456 * 10000.
            (byte)'T'
        ];
        serializer.Serialize(bytes, record, context);
        var roundtrip = serializer.Deserialize(bytes, context);

        Assert.Equal(expected, bytes);
        Assert.Equal(record, roundtrip);

        var beforeWrite = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
            serializer.Serialize(bytes, record, context);
        var writeAllocation = GC.GetAllocatedBytesForCurrentThread() - beforeWrite;

        // Deserialization needs one array to retain the fields, but no boxes or temporary object[].
        var beforeArray = GC.GetAllocatedBytesForCurrentThread();
        var storage = AllocateFields(record.Count);
        var arrayAllocation = GC.GetAllocatedBytesForCurrentThread() - beforeArray;
        GC.KeepAlive(storage);
        var beforeRead = GC.GetAllocatedBytesForCurrentThread();
        var idSum = 0;
        for (var i = 0; i < 128; i++)
            idSum += (int)serializer.Deserialize(bytes, context)[0];
        var readAllocation = GC.GetAllocatedBytesForCurrentThread() - beforeRead;

        Assert.Equal(0, writeAllocation);
        Assert.Equal(arrayAllocation * 128, readAllocation);
        Assert.Equal(42 * 128, idSum);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static DbfField[] AllocateFields(int count) => new DbfField[count];

    private static string GetTempDbfPath() =>
        Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dbf");

    private static void DeleteIfExists(string dbfPath)
    {
        File.Delete(dbfPath);
        File.Delete(Path.ChangeExtension(dbfPath, "dbt"));
        File.Delete(Path.ChangeExtension(dbfPath, "fpt"));
    }

    private sealed class MutableCustomer
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    private sealed class NullableCurrencyRecord
    {
        public decimal? Amount { get; set; }
    }

    private sealed class GeneratedBinaryLikeRecord
    {
        public byte[] Fixed { get; set; } = [];

        public byte[] Payload { get; set; } = [];

        public byte[] _NullFlags { get; set; } = [];
    }
}
