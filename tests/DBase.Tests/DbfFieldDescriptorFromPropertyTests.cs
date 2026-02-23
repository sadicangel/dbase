using System.Reflection;

namespace DBase.Tests;

public sealed class DbfFieldDescriptorFromPropertyTests
{
    private static readonly MethodInfo s_fromProperty =
        typeof(DbfFieldDescriptor).GetMethod(
            "FromProperty",
            BindingFlags.Static | BindingFlags.NonPublic,
            [typeof(PropertyInfo), typeof(DbfVersion)])
        ?? throw new InvalidOperationException("DbfFieldDescriptor.FromProperty was not found.");

    [Theory]
    [InlineData(nameof(SupportedModel.StringValue), DbfFieldType.Character, 254, 0)]
    [InlineData(nameof(SupportedModel.CharArrayValue), DbfFieldType.Character, 254, 0)]
    [InlineData(nameof(SupportedModel.ReadOnlyMemoryCharValue), DbfFieldType.Character, 254, 0)]
    [InlineData(nameof(SupportedModel.Int32Value), DbfFieldType.Float, 10, 0)]
    [InlineData(nameof(SupportedModel.Int32NullableValue), DbfFieldType.Float, 10, 0)]
    [InlineData(nameof(SupportedModel.UInt32Value), DbfFieldType.Float, 10, 0)]
    [InlineData(nameof(SupportedModel.UInt32NullableValue), DbfFieldType.Float, 10, 0)]
    [InlineData(nameof(SupportedModel.Int64Value), DbfFieldType.Float, 20, 0)]
    [InlineData(nameof(SupportedModel.Int64NullableValue), DbfFieldType.Float, 20, 0)]
    [InlineData(nameof(SupportedModel.UInt64Value), DbfFieldType.Float, 20, 0)]
    [InlineData(nameof(SupportedModel.UInt64NullableValue), DbfFieldType.Float, 20, 0)]
    [InlineData(nameof(SupportedModel.SingleValue), DbfFieldType.Float, 20, 8)]
    [InlineData(nameof(SupportedModel.SingleNullableValue), DbfFieldType.Float, 20, 8)]
    [InlineData(nameof(SupportedModel.DoubleValue), DbfFieldType.Float, 20, 8)]
    [InlineData(nameof(SupportedModel.DoubleNullableValue), DbfFieldType.Float, 20, 8)]
    [InlineData(nameof(SupportedModel.DecimalValue), DbfFieldType.Currency, 8, 0)]
    [InlineData(nameof(SupportedModel.DecimalNullableValue), DbfFieldType.Currency, 8, 0)]
    [InlineData(nameof(SupportedModel.BooleanValue), DbfFieldType.Logical, 1, 0)]
    [InlineData(nameof(SupportedModel.BooleanNullableValue), DbfFieldType.Logical, 1, 0)]
    [InlineData(nameof(SupportedModel.DateOnlyValue), DbfFieldType.Date, 8, 0)]
    [InlineData(nameof(SupportedModel.DateOnlyNullableValue), DbfFieldType.Date, 8, 0)]
    [InlineData(nameof(SupportedModel.DateTimeValue), DbfFieldType.DateTime, 8, 0)]
    [InlineData(nameof(SupportedModel.DateTimeNullableValue), DbfFieldType.DateTime, 8, 0)]
    [InlineData(nameof(SupportedModel.DateTimeOffsetValue), DbfFieldType.DateTime, 8, 0)]
    [InlineData(nameof(SupportedModel.DateTimeOffsetNullableValue), DbfFieldType.DateTime, 8, 0)]
    public void FromProperty_SupportedType_ReturnsExpectedDescriptor(
        string propertyName,
        DbfFieldType expectedFieldType,
        int expectedLength,
        int expectedDecimal)
    {
        var descriptor = InvokeFromProperty(propertyName, DbfVersion.VisualFoxPro);

        Assert.Equal(expectedFieldType, descriptor.Type);
        Assert.Equal((byte)expectedLength, descriptor.Length);
        Assert.Equal((byte)expectedDecimal, descriptor.Decimal);
    }

    [Theory]
    [InlineData(nameof(UnsupportedModel.ByteArrayValue))]
    [InlineData(nameof(UnsupportedModel.MemoryByteValue))]
    [InlineData(nameof(UnsupportedModel.ReadOnlyMemoryByteValue))]
    public void FromProperty_UnsupportedType_ThrowsArgumentException(string propertyName)
    {
        var property = typeof(UnsupportedModel).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");

        var exception = Assert.Throws<TargetInvocationException>(() => s_fromProperty.Invoke(null, [property, DbfVersion.VisualFoxPro]));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void FromProperty_DecimalForNonFoxPro_ThrowsArgumentException()
    {
        var property = typeof(SupportedModel).GetProperty(nameof(SupportedModel.DecimalValue))
            ?? throw new InvalidOperationException($"Property '{nameof(SupportedModel.DecimalValue)}' was not found.");

        var exception = Assert.Throws<TargetInvocationException>(() => s_fromProperty.Invoke(null, [property, DbfVersion.DBase03]));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void FromProperty_DateTimeOffsetForNonFoxPro_ThrowsArgumentException()
    {
        var property = typeof(SupportedModel).GetProperty(nameof(SupportedModel.DateTimeOffsetValue))
            ?? throw new InvalidOperationException($"Property '{nameof(SupportedModel.DateTimeOffsetValue)}' was not found.");

        var exception = Assert.Throws<TargetInvocationException>(() => s_fromProperty.Invoke(null, [property, DbfVersion.DBase03]));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void FromProperty_DateTimeForNonFoxPro_UsesDateField()
    {
        var descriptor = InvokeFromProperty(nameof(SupportedModel.DateTimeValue), DbfVersion.DBase03);
        Assert.Equal(DbfFieldType.Date, descriptor.Type);
        Assert.Equal((byte)8, descriptor.Length);
        Assert.Equal((byte)0, descriptor.Decimal);
    }

    private static DbfFieldDescriptor InvokeFromProperty(string propertyName, DbfVersion version)
    {
        var property = typeof(SupportedModel).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found.");

        return (DbfFieldDescriptor)(s_fromProperty.Invoke(null, [property, version])
            ?? throw new InvalidOperationException("FromProperty returned null."));
    }

    private sealed class SupportedModel
    {
        public string StringValue { get; set; } = string.Empty;
        public char[] CharArrayValue { get; set; } = [];
        public ReadOnlyMemory<char> ReadOnlyMemoryCharValue { get; set; } = ReadOnlyMemory<char>.Empty;
        public int Int32Value { get; set; }
        public int? Int32NullableValue { get; set; }
        public uint UInt32Value { get; set; }
        public uint? UInt32NullableValue { get; set; }
        public long Int64Value { get; set; }
        public long? Int64NullableValue { get; set; }
        public ulong UInt64Value { get; set; }
        public ulong? UInt64NullableValue { get; set; }
        public float SingleValue { get; set; }
        public float? SingleNullableValue { get; set; }
        public double DoubleValue { get; set; }
        public double? DoubleNullableValue { get; set; }
        public decimal DecimalValue { get; set; }
        public decimal? DecimalNullableValue { get; set; }
        public bool BooleanValue { get; set; }
        public bool? BooleanNullableValue { get; set; }
        public DateOnly DateOnlyValue { get; set; }
        public DateOnly? DateOnlyNullableValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTime? DateTimeNullableValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public DateTimeOffset? DateTimeOffsetNullableValue { get; set; }
    }

    private sealed class UnsupportedModel
    {
        public byte[] ByteArrayValue { get; set; } = [];
        public Memory<byte> MemoryByteValue { get; set; } = Memory<byte>.Empty;
        public ReadOnlyMemory<byte> ReadOnlyMemoryByteValue { get; set; } = ReadOnlyMemory<byte>.Empty;
    }
}
