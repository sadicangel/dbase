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
    public void CreateTyped_NullableCurrencyWithNull_ThrowsSerializationException()
    {
        var dbfPath = GetTempDbfPath();

        try
        {
            using var source = Dbf.Create<NullableCurrencyRecord>(dbfPath, new DbfCreateOptions { Version = DbfVersion.VisualFoxPro });

            var exception = Assert.Throws<DbfSerializationException>(
                () => source.Add(new NullableCurrencyRecord { Amount = null }));

            Assert.Equal(DbfSerializationOperation.Write, exception.Operation);
            Assert.Equal(nameof(NullableCurrencyRecord.Amount), exception.FieldName.ToString());
            Assert.Equal(typeof(decimal?), exception.TargetClrType);
            var inner = Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Contains("Currency fields cannot serialize null values", inner.Message);
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

    [Theory]
    [MemberData(nameof(BadFieldReadCases))]
    public void GetRecord_InvalidFieldValue_ThrowsSerializationException(BadFieldReadCase testCase)
    {
        var dbfBytes = new MemoryStream();
        using var dbf = Dbf.Create(dbfBytes, testCase.Descriptors);
        testCase.AddValidRecord(dbf);
        OverwriteField(dbfBytes, dbf, testCase.FieldIndex, testCase.InvalidBytes);

        var exception = Assert.Throws<DbfSerializationException>(() => testCase.ReadInvalidRecord(dbf));

        Assert.Equal(DbfSerializationOperation.Read, exception.Operation);
        Assert.Equal(0, exception.RecordIndex);
        Assert.Equal(testCase.FieldIndex, exception.FieldIndex);
        Assert.Equal(testCase.FieldName, exception.FieldName.ToString());
        Assert.Equal(testCase.FieldType, exception.FieldType);
        Assert.Equal(testCase.TargetClrType, exception.TargetClrType);
        Assert.Equal(testCase.RecordType, exception.RecordType);
        Assert.Contains("read", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"record {exception.RecordIndex}", exception.Message);
        Assert.Contains(testCase.FieldName, exception.Message);
        Assert.Contains(testCase.FieldType.ToString(), exception.Message);
        Assert.Contains(testCase.TargetClrType.FullName!, exception.Message);
        Assert.IsType(testCase.InnerExceptionType, exception.InnerException);
    }

    [Fact]
    public void Add_NumericValueTooLong_ThrowsSerializationExceptionWithDescriptorLength()
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Numeric("COUNT", 3));
        using var dbf = Dbf.Create(new MemoryStream(), descriptors);

        var exception = Assert.Throws<DbfSerializationException>(
            () => dbf.Add(new NumericRecord(12345)));

        Assert.Equal(DbfSerializationOperation.Write, exception.Operation);
        Assert.Equal(0, exception.RecordIndex);
        Assert.Equal(0, exception.FieldIndex);
        Assert.Equal("COUNT", exception.FieldName.ToString());
        Assert.Equal((byte)3, exception.Descriptor.Length);
        Assert.Equal(typeof(int), exception.TargetClrType);
        Assert.IsType<OverflowException>(exception.InnerException);
        Assert.Contains("COUNT", exception.Message);
        Assert.Contains("length 3", exception.Message);
    }

    [Fact]
    public void GetSerializer_PropertyCountMismatch_MessageListsDescriptorsPropertiesAndRecordType()
    {
        var descriptors = ImmutableArray.Create(
            DbfFieldDescriptor.Character("NAME", 10),
            DbfFieldDescriptor.Numeric("AGE"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => descriptors.GetSerializer<PropertyCountMismatchRecord>());

        Assert.Contains(typeof(PropertyCountMismatchRecord).FullName!, exception.Message);
        Assert.Contains("NAME", exception.Message);
        Assert.Contains("AGE", exception.Message);
        Assert.Contains(nameof(PropertyCountMismatchRecord.Name), exception.Message);
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
        var context = new DbfSerializationContext(Encoding.ASCII, null, '.',
            DbfSerializationOperation.Write, 0, dbf.Version, dbf.Language, typeof(DbfRecord));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetRecord_InvalidSecondFieldInSecondRecord_ReportsPhysicalContext(bool untyped)
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 3),
            DbfFieldDescriptor.Numeric("COUNT", 5), DbfFieldDescriptor.Logical("FLAG"));
        using var bytes = new MemoryStream();
        using var dbf = Dbf.Create(bytes, descriptors);
        dbf.Add(new ContextRecord("one", 123, true));
        dbf.Add(new ContextRecord("two", 456, false));
        bytes.Position = dbf.HeaderLength;
        bytes.WriteByte((byte)DbfRecordStatus.Deleted);
        bytes.Position = dbf.HeaderLength + dbf.RecordLength + dbf.Descriptors[1].Offset;
        bytes.Write("abc  "u8);

        var exception = Assert.Throws<DbfSerializationException>(() =>
        {
            if (untyped) dbf.GetRecord(1);
            else dbf.GetRecord<ContextRecord>(1);
        });

        Assert.Equal(DbfSerializationOperation.Read, exception.Operation);
        Assert.Equal(1, exception.RecordIndex);
        Assert.Equal(1, exception.FieldIndex);
        Assert.Equal(dbf.Descriptors[1], exception.Descriptor);
        Assert.Equal("COUNT", exception.FieldName.ToString());
        Assert.Equal(DbfFieldType.Numeric, exception.FieldType);
        Assert.Equal(untyped ? typeof(DbfField) : typeof(int), exception.TargetClrType);
        Assert.Equal(untyped ? typeof(DbfRecord) : typeof(ContextRecord), exception.RecordType);
        Assert.Equal(dbf.Version, exception.Version);
        Assert.Equal(dbf.Language, exception.Language);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Add_SecondFieldOverflowInSecondRecord_ReportsContextWithoutAppending(bool untyped)
    {
        var descriptors = ImmutableArray.Create(DbfFieldDescriptor.Character("NAME", 3),
            DbfFieldDescriptor.Numeric("COUNT", 3), DbfFieldDescriptor.Logical("FLAG"));
        using var dbf = Dbf.Create(new MemoryStream(), descriptors);
        dbf.Add(new ContextRecord("one", 123, true));

        var exception = Assert.Throws<DbfSerializationException>(() =>
        {
            if (untyped) dbf.Add(new DbfRecord("two", 12345L, false));
            else dbf.Add(new ContextRecord("two", 12345, false));
        });

        Assert.Equal(DbfSerializationOperation.Write, exception.Operation);
        Assert.Equal(1, exception.RecordIndex);
        Assert.Equal(1, exception.FieldIndex);
        Assert.Equal(dbf.Descriptors[1], exception.Descriptor);
        Assert.Equal(untyped ? typeof(DbfField) : typeof(int), exception.TargetClrType);
        Assert.Equal(untyped ? typeof(DbfRecord) : typeof(ContextRecord), exception.RecordType);
        Assert.Equal(dbf.Version, exception.Version);
        Assert.Equal(dbf.Language, exception.Language);
        Assert.IsType<OverflowException>(exception.InnerException);
        Assert.Equal(1, dbf.RecordCount);
        Assert.Equal(new ContextRecord("one", 123, true), dbf.GetRecord<ContextRecord>(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Deserialize_UnexpectedFailure_PropagatesOriginalException(bool untyped)
    {
        using var dbf = Dbf.Create(new MemoryStream(), [DbfFieldDescriptor.Numeric("COUNT", 5)]);
        var typedSerializer = new DbfRecordSerializer<NumericRecord>(dbf.Descriptors);
        var untypedSerializer = new DbfRecordSerializer<DbfRecord>(dbf.Descriptors);
        var bytes = "  12345"u8.ToArray();
        Exception[] failures = [new OperationCanceledException(), new OutOfMemoryException("sentinel"),
            new NullReferenceException("sentinel")];

        foreach (var failure in failures)
        {
            var context = new DbfSerializationContext(new ThrowingEncoding(failure), null, '.',
                DbfSerializationOperation.Read, 0, dbf.Version, dbf.Language,
                untyped ? typeof(DbfRecord) : typeof(NumericRecord));

            var actual = Record.Exception(() =>
            {
                if (untyped) untypedSerializer.Deserialize(bytes, in context);
                else typedSerializer.Deserialize(bytes, in context);
            });

            Assert.Same(failure, actual);
        }
    }

    private sealed class ThrowingEncoding(Exception failure) : UTF8Encoding
    {
        public override int GetCharCount(ReadOnlySpan<byte> bytes) => throw failure;
    }

    private sealed record ContextRecord(string Name, int Count, bool Flag);

    private static string GetTempDbfPath() =>
        Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dbf");

    private static void DeleteIfExists(string dbfPath)
    {
        File.Delete(dbfPath);
        File.Delete(Path.ChangeExtension(dbfPath, "dbt"));
        File.Delete(Path.ChangeExtension(dbfPath, "fpt"));
    }

    public static IEnumerable<object[]> BadFieldReadCases()
    {
        yield return
        [
            new BadFieldReadCase(
                ImmutableArray.Create(DbfFieldDescriptor.Numeric("COUNT", 5)),
                0,
                "COUNT",
                DbfFieldType.Numeric,
                typeof(int),
                typeof(NumericRecord),
                typeof(FormatException),
                "abc  "u8.ToArray(),
                static dbf => dbf.Add(new NumericRecord(123)),
                static dbf => dbf.GetRecord<NumericRecord>(0))
        ];

        yield return
        [
            new BadFieldReadCase(
                ImmutableArray.Create(DbfFieldDescriptor.Date("WHEN")),
                0,
                "WHEN",
                DbfFieldType.Date,
                typeof(DateOnly),
                typeof(DateRecord),
                typeof(FormatException),
                "20231301"u8.ToArray(),
                static dbf => dbf.Add(new DateRecord(new DateOnly(2024, 1, 1))),
                static dbf => dbf.GetRecord<DateRecord>(0))
        ];

        yield return
        [
            new BadFieldReadCase(
                ImmutableArray.Create(DbfFieldDescriptor.Logical("FLAG")),
                0,
                "FLAG",
                DbfFieldType.Logical,
                typeof(bool),
                typeof(LogicalRecord),
                typeof(InvalidOperationException),
                "X"u8.ToArray(),
                static dbf => dbf.Add(new LogicalRecord(true)),
                static dbf => dbf.GetRecord<LogicalRecord>(0))
        ];
    }

    private static void OverwriteField(MemoryStream dbfBytes, Dbf dbf, int fieldIndex, byte[] bytes)
    {
        var descriptor = dbf.Descriptors[fieldIndex];
        Assert.Equal(descriptor.Length, bytes.Length);

        dbfBytes.Position = dbf.HeaderLength + descriptor.Offset;
        dbfBytes.Write(bytes);
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

    public sealed record BadFieldReadCase(
        ImmutableArray<DbfFieldDescriptor> Descriptors,
        int FieldIndex,
        string FieldName,
        DbfFieldType FieldType,
        Type TargetClrType,
        Type RecordType,
        Type InnerExceptionType,
        byte[] InvalidBytes,
        Action<Dbf> AddValidRecord,
        Action<Dbf> ReadInvalidRecord);

    private sealed record NumericRecord(int Count);

    private sealed record DateRecord(DateOnly When);

    private sealed record LogicalRecord(bool Flag);

    private sealed class PropertyCountMismatchRecord
    {
        public string Name { get; set; } = string.Empty;
    }
}
