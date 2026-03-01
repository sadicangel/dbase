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
        var expected = new MultiField { A = 1, B = 2, C = 3, D = 4 };
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
    public void ReverseFieldBytes_TwiceRoundTrips()
    {
        var original = new MultiField { A = 0x01020304, B = 0x0506, C = 0x0708, D = 0x090A0B0C };
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<MultiField>()];
        MemoryMarshal.Write(buffer, in original);

        var expected = buffer.ToArray();

        StreamExtensions.ReverseFieldBytes<MultiField>(buffer);
        StreamExtensions.ReverseFieldBytes<MultiField>(buffer);

        Assert.Equal(expected, buffer.ToArray());
    }

    [Fact]
    public void ReverseFieldBytes_ReversesEachFieldIndependently()
    {
        var value = new MultiField { A = 0x01020304, B = 0x0506, C = 0x0708, D = 0x090A0B0C };
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<MultiField>()];
        MemoryMarshal.Write(buffer, in value);

        var before = buffer.ToArray();

        StreamExtensions.ReverseFieldBytes<MultiField>(buffer);

        // Each field's bytes should be individually reversed
        Assert.Equal(before[0..4].Reverse().ToArray(), buffer[0..4].ToArray());   // A: int
        Assert.Equal(before[4..6].Reverse().ToArray(), buffer[4..6].ToArray());   // B: short
        Assert.Equal(before[6..8].Reverse().ToArray(), buffer[6..8].ToArray());   // C: short
        Assert.Equal(before[8..12].Reverse().ToArray(), buffer[8..12].ToArray()); // D: int
    }

    [Fact]
    public void ReverseFieldBytes_SingleByte_IsNoOp()
    {
        var value = new ByteField { Value = 0xAB };
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<ByteField>()];
        MemoryMarshal.Write(buffer, in value);

        var expected = buffer.ToArray();

        StreamExtensions.ReverseFieldBytes<ByteField>(buffer);

        Assert.Equal(expected, buffer.ToArray());
    }

    private static MemoryStream WriteToStream<T>(T value) where T : unmanaged
    {
        var stream = new MemoryStream();
        stream.Write(value);
        stream.Position = 0;
        return stream;
    }
}
