using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DBase.Interop;

namespace DBase.Tests;

public class StreamExtensionsTests
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct SingleField
    {
        [FieldOffset(0)] public int Value;
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    private struct MultiField
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(4)] public short B;
        [FieldOffset(6)] public short C;
        [FieldOffset(8)] public int D;
    }

    [StructLayout(LayoutKind.Explicit, Size = 1)]
    private struct ByteField
    {
        [FieldOffset(0)] public byte Value;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct NestedFields
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(4)] public short B;
        [FieldOffset(6)] public byte C;
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    private struct StructField
    {
        [FieldOffset(0)] public byte Prefix;
        [FieldOffset(2)] public NestedFields Nested;
        [FieldOffset(10)] public short Suffix;
    }

    [InlineArray(3)]
    private struct Int32InlineArray
    {
        private int _e0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct InlineArrayField
    {
        [FieldOffset(0)] public byte Prefix;
        [FieldOffset(4)] public Int32InlineArray Values;
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct OverlappingScalarFields
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(0)] public short B;
    }

    [Fact]
    public void Read_SingleField_RoundTrips()
    {
        var expected = new SingleField { Value = 0x12345678 };
        var stream = WriteToStream(expected);

        var actual = stream.Read<SingleField>();

        Assert.Equal(expected.Value, actual.Value);
    }

    [Fact]
    public void Read_MultiField_RoundTrips()
    {
        var expected = new MultiField
        {
            A = 1,
            B = 2,
            C = 3,
            D = 4
        };
        var stream = WriteToStream(expected);

        var actual = stream.Read<MultiField>();

        Assert.Equal(expected.A, actual.A);
        Assert.Equal(expected.B, actual.B);
        Assert.Equal(expected.C, actual.C);
        Assert.Equal(expected.D, actual.D);
    }

    [Fact]
    public void Read_ByteField_RoundTrips()
    {
        var expected = new ByteField { Value = 0xAB };
        var stream = WriteToStream(expected);

        var actual = stream.Read<ByteField>();

        Assert.Equal(expected.Value, actual.Value);
    }

    [Fact]
    public void TryRead_ReturnsFalse_WhenStreamIsEmpty()
    {
        var stream = new MemoryStream();

        var result = stream.TryRead<SingleField>(out _);

        Assert.False(result);
    }

    [Fact]
    public void TryRead_ReturnsFalse_WhenStreamIsTooShort()
    {
        var stream = new MemoryStream([0x01, 0x02]);

        var result = stream.TryRead<SingleField>(out _);

        Assert.False(result);
    }

    [Fact]
    public void Read_ThrowsEndOfStreamException_WhenStreamIsEmpty()
    {
        var stream = new MemoryStream();

        Assert.Throws<EndOfStreamException>(() => stream.Read<SingleField>());
    }

    [Fact]
    public void Write_ThenRead_MultipleStructs_InSequence()
    {
        var stream = new MemoryStream();
        var s1 = new SingleField { Value = 42 };
        var s2 = new SingleField { Value = -1 };

        stream.Write(s1);
        stream.Write(s2);
        stream.Position = 0;

        var r1 = stream.Read<SingleField>();
        var r2 = stream.Read<SingleField>();

        Assert.Equal(s1.Value, r1.Value);
        Assert.Equal(s2.Value, r2.Value);
    }

    [Fact]
    public void ReverseEndianness_TwiceRoundTrips()
    {
        var value = CreateAscendingStruct<MultiField>();
        var expected = GetBytes(value);

        value.ReverseEndianness();
        value.ReverseEndianness();

        Assert.Equal(expected, GetBytes(value));
    }

    [Fact]
    public void ReverseEndianness_ReversesScalarFieldsIndependently() =>
        AssertReverseEndianness<MultiField>((0, 4), (4, 2), (6, 2), (8, 4));

    [Fact]
    public void ReverseEndianness_SingleByte_IsNoOp()
    {
        var value = CreateAscendingStruct<ByteField>();
        var expected = GetBytes(value);

        value.ReverseEndianness();

        Assert.Equal(expected, GetBytes(value));
    }

    [Fact]
    public void ReverseEndianness_StructField_ReversesNestedScalarFields() =>
        AssertReverseEndianness<StructField>((2, 4), (6, 2), (10, 2));

    [Fact]
    public void ReverseEndianness_InlineArrayField_ReversesEachElement() =>
        AssertReverseEndianness<InlineArrayField>((4, 4), (8, 4), (12, 4));

    [Fact]
    public void ReverseEndianness_DbfHeader_ReversesOnlyMultiByteNumericFields() =>
        AssertReverseEndianness<DbfHeader>((4, 4), (8, 2), (10, 2), (30, 2));

    [Fact]
    public void ReverseEndianness_DbfHeader02_ReversesOnlyMultiByteNumericFields() =>
        AssertReverseEndianness<DbfHeader02>((1, 2), (6, 2));

    [Fact]
    public void ReverseEndianness_DbfFieldDescriptor_PreservesNameBytes() =>
        AssertReverseEndianness<DbfFieldDescriptor>((12, 4), (19, 4), (24, 8));

    [Fact]
    public void ReverseEndianness_DbfFieldDescriptor02_PreservesNameAndReservedBytes() =>
        AssertReverseEndianness<DbfFieldDescriptor02>((13, 2));

    [Fact]
    public void ReverseEndianness_DbtHeader_ReversesReservedScalarFields() =>
        AssertReverseEndianness<DbtHeader>((0, 4), (4, 2), (6, 2), (8, 8), (16, 4), (20, 2), (22, 2));

    [Fact]
    public void ReverseEndianness_FptHeader_ReversesReservedScalarFields() =>
        AssertReverseEndianness<FptHeader>((0, 4), (4, 2), (6, 2));

    [Fact]
    public void ReverseEndianness_OverlappingScalarFields_Throws()
    {
        var value = CreateAscendingStruct<OverlappingScalarFields>();

        var exception = Assert.Throws<InvalidOperationException>(() => value.ReverseEndianness());

        Assert.Contains(nameof(OverlappingScalarFields), exception.Message);
    }

    private static MemoryStream WriteToStream<T>(T value) where T : unmanaged
    {
        var stream = new MemoryStream();
        stream.Write(value);
        stream.Position = 0;
        return stream;
    }

    private static void AssertReverseEndianness<T>(params (int Offset, int Length)[] ranges) where T : unmanaged
    {
        var value = CreateAscendingStruct<T>();
        var expected = GetBytes(value);

        foreach (var (offset, length) in ranges)
            Array.Reverse(expected, offset, length);

        value.ReverseEndianness();

        Assert.Equal(expected, GetBytes(value));
    }

    private static T CreateAscendingStruct<T>() where T : unmanaged
    {
        var buffer = new byte[Unsafe.SizeOf<T>()];
        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = (byte)i;

        return MemoryMarshal.Read<T>(buffer);
    }

    private static byte[] GetBytes<T>(T value) where T : unmanaged
    {
        var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        return buffer.ToArray();
    }
}
