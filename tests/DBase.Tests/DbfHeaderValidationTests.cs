using System.Buffers.Binary;

namespace DBase.Tests;

public class DbfHeaderValidationTests
{
    [Fact]
    public void Open_EmptyStream_ThrowsEndOfStreamException()
    {
        using var stream = new MemoryStream();

        var exception = Assert.Throws<EndOfStreamException>(() => Dbf.Open(stream));

        Assert.Contains("header", exception.Message);
    }

    [Fact]
    public void Open_ShortHeader_ThrowsEndOfStreamException()
    {
        var bytes = new byte[DbfHeader.Size - 1];
        bytes[0] = (byte)DbfVersion.DBase03;
        using var stream = new MemoryStream(bytes);

        Assert.Throws<EndOfStreamException>(() => Dbf.Open(stream));
    }

    [Fact]
    public void Open_UnsupportedVersion_ThrowsNotSupportedException()
    {
        using var stream = new MemoryStream([0x7F]);

        var exception = Assert.Throws<NotSupportedException>(() => Dbf.Open(stream));

        Assert.Contains("0x7F", exception.Message);
    }

    [Fact]
    public void Open_MissingDescriptorTerminator_ThrowsInvalidDataException()
    {
        var bytes = CreateHeader(DbfHeader.Size + DbfFieldDescriptor.Size, recordLength: 2);
        WriteCharacterDescriptor(bytes, DbfHeader.Size, length: 1);
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidDataException>(() => Dbf.Open(stream));

        Assert.Contains("terminator", exception.Message);
    }

    [Fact]
    public void Open_DescriptorBeyondHeaderLength_ThrowsInvalidDataException()
    {
        var bytes = CreateHeader(DbfHeader.Size + 8, recordLength: 1);
        bytes[DbfHeader.Size] = (byte)'F';
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidDataException>(() => Dbf.Open(stream));

        Assert.Contains("descriptor", exception.Message);
    }

    [Fact]
    public void Open_TerminatorBeforeDeclaredHeaderEnd_ThrowsInvalidDataException()
    {
        var bytes = CreateHeader(DbfHeader.Size + DbfFieldDescriptor.Size + 2, recordLength: 2);
        WriteCharacterDescriptor(bytes, DbfHeader.Size, length: 1);
        bytes[DbfHeader.Size + DbfFieldDescriptor.Size] = 0x0D;
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidDataException>(() => Dbf.Open(stream));

        Assert.Contains("header length", exception.Message);
    }

    [Fact]
    public void Open_MissingFoxProTerminatorBeforeBacklink_ThrowsInvalidDataException()
    {
        var headerLength = DbfHeader.Size + DbfFieldDescriptor.Size + 1 + DbcBacklink.Size;
        var bytes = CreateHeader(headerLength, recordLength: 2, version: DbfVersion.VisualFoxPro);
        WriteCharacterDescriptor(bytes, DbfHeader.Size, length: 1);
        bytes[DbfHeader.Size + DbfFieldDescriptor.Size + 1] = 0x0D;
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidDataException>(() => Dbf.Open(stream));

        Assert.Contains("descriptor", exception.Message);
    }

    [Fact]
    public void Open_RecordLengthDoesNotMatchDescriptors_ThrowsInvalidDataException()
    {
        var bytes = CreateHeader(DbfHeader.Size + DbfFieldDescriptor.Size + 1, recordLength: 1);
        WriteCharacterDescriptor(bytes, DbfHeader.Size, length: 1);
        bytes[DbfHeader.Size + DbfFieldDescriptor.Size] = 0x0D;
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<InvalidDataException>(() => Dbf.Open(stream));

        Assert.Contains("record length", exception.Message);
    }

    [Fact]
    public void Open_TruncatedRecordData_ThrowsEndOfStreamException()
    {
        var bytes = CreateHeader(DbfHeader.Size + 1, recordLength: 1, recordCount: 1);
        bytes[DbfHeader.Size] = 0x0D;
        using var stream = new MemoryStream(bytes);

        var exception = Assert.Throws<EndOfStreamException>(() => Dbf.Open(stream));

        Assert.Contains("records", exception.Message);
    }

    private static byte[] CreateHeader(int headerLength, int recordLength, int recordCount = 0, DbfVersion version = DbfVersion.DBase03)
    {
        var bytes = new byte[headerLength];
        bytes[0] = (byte)version;
        bytes[1] = 126;
        bytes[2] = 1;
        bytes[3] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)recordCount);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), (ushort)headerLength);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10), (ushort)recordLength);
        return bytes;
    }

    private static void WriteCharacterDescriptor(byte[] bytes, int offset, byte length)
    {
        bytes[offset] = (byte)'F';
        bytes[offset + 11] = (byte)DbfFieldType.Character;
        bytes[offset + 16] = length;
    }
}
