using System.Collections.Immutable;
using DBase.CodeGen;

namespace DBase.Tests.CodeGen;

public sealed class RecordGeneratorTests
{
    [Fact]
    public void GenerateRecord_BinaryMemoAndNullFlagsFields_UsesSerializerSupportedTypes()
    {
        var dbfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dbf");
        try
        {
            var descriptors = ImmutableArray.Create(
                DbfFieldDescriptor.Binary("BinDouble", length: 8),
                DbfFieldDescriptor.Binary("BinMemo"),
                DbfFieldDescriptor.Blob("BlobData"),
                DbfFieldDescriptor.NullFlags("_NullFlags", length: 1),
                DbfFieldDescriptor.Ole("OleData"),
                DbfFieldDescriptor.Picture("PicData"));

            using (Dbf.Create(dbfPath, descriptors, new DbfCreateOptions { Version = DbfVersion.VisualFoxPro }))
            {
            }

            var source = RecordGenerator.GenerateRecord(dbfPath);

            Assert.Contains("double BinDouble,", source);
            Assert.Contains("string BinMemo,", source);
            Assert.Contains("string BlobData,", source);
            Assert.Contains("string NullFlags,", source);
            Assert.Contains("string OleData,", source);
            Assert.Contains("string PicData", source);
            Assert.DoesNotContain("byte[]", source);
        }
        finally
        {
            File.Delete(dbfPath);
            File.Delete(Path.ChangeExtension(dbfPath, "dbt"));
            File.Delete(Path.ChangeExtension(dbfPath, "fpt"));
        }
    }

    [Fact]
    public void GenerateClass_BinaryMemoAndNullFlagsFields_UsesSerializerSupportedTypes()
    {
        var dbfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dbf");
        try
        {
            var descriptors = ImmutableArray.Create(
                DbfFieldDescriptor.Binary("BinDouble", length: 8),
                DbfFieldDescriptor.Binary("BinMemo"),
                DbfFieldDescriptor.Blob("BlobData"),
                DbfFieldDescriptor.NullFlags("_NullFlags", length: 1),
                DbfFieldDescriptor.Ole("OleData"),
                DbfFieldDescriptor.Picture("PicData"));

            using (Dbf.Create(dbfPath, descriptors, new DbfCreateOptions { Version = DbfVersion.VisualFoxPro }))
            {
            }

            var source = RecordGenerator.GenerateClass(dbfPath);

            Assert.Contains("public double BinDouble { get; set; }", source);
            Assert.Contains("public string BinMemo { get; set; }", source);
            Assert.Contains("public string BlobData { get; set; }", source);
            Assert.Contains("public string NullFlags { get; set; }", source);
            Assert.Contains("public string OleData { get; set; }", source);
            Assert.Contains("public string PicData { get; set; }", source);
            Assert.DoesNotContain("byte[]", source);
        }
        finally
        {
            File.Delete(dbfPath);
            File.Delete(Path.ChangeExtension(dbfPath, "dbt"));
            File.Delete(Path.ChangeExtension(dbfPath, "fpt"));
        }
    }
}
