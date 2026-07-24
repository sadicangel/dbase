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

            using (Dbf.Create(dbfPath, descriptors, DbfVersion.VisualFoxPro))
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
}
