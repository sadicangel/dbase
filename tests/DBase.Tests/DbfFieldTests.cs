using System.Runtime.CompilerServices;

using System.Globalization;

namespace DBase.Tests;

public sealed class DbfFieldTests
{
    [Fact]
    public void IsNull_NullField_ReturnsTrue()
    {
        DbfField field = (string?)null;

        Assert.True(field.IsNull);
        Assert.False(field.IsType<string>());
    }

    [Fact]
    public void IsType_ExactStoredType_ReturnsTrue()
    {
        DbfField field = 42;

        Assert.False(field.IsNull);
        Assert.True(field.IsType<int>());
        Assert.False(field.IsType<long>());
        Assert.False(field.IsType<int?>());
        Assert.False(field.IsType<object>());
        Assert.False(field.IsType<IComparable>());
        Assert.Equal(42, field.GetValue<object>());
    }

    [Fact]
    public void IsNull_DefaultAndNullableCases_ReturnsTrue()
    {
        DbfField[] fields = [default, (bool?)null, (int?)null, (long?)null,
            (double?)null, (decimal?)null, (DateTime?)null, (string?)null];

        foreach (var field in fields)
        {
            Assert.True(field.IsNull);
            Assert.Null(field.Value);
            Assert.Equal(default, field);
            Assert.Null((bool?)field);
            Assert.Null((int?)field);
            Assert.Null((long?)field);
            Assert.Null((double?)field);
            Assert.Null((decimal?)field);
            Assert.Null((DateTime?)field);
            Assert.Null((string?)field);
        }
    }

    [Fact]
    public void Value_AllCases_PreservesStoredTypeAndValue()
    {
        var dateTime = new DateTime(2026, 10, 9, 12, 34, 56, DateTimeKind.Utc);
        (DbfField Field, object Expected)[] cases =
        [
            (false, false), (true, true), (int.MinValue, int.MinValue),
            (long.MaxValue, long.MaxValue), (1.23456789D, 1.23456789D),
            (double.MaxValue, double.MaxValue), (double.NaN, double.NaN),
            (double.NegativeInfinity, double.NegativeInfinity),
            (123.456789M, 123.456789M), (dateTime, dateTime), ("", ""), ("text", "text")
        ];

        foreach (var (field, expected) in cases)
        {
            Assert.False(field.IsNull);
            Assert.IsType(expected.GetType(), field.Value);
            Assert.Equal(expected, field.Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Conversion_DecimalAllScales_PreservesCoefficientScaleAndSign(bool negative)
    {
        for (byte scale = 0; scale <= 28; scale++)
        {
            // Exercise all 96 coefficient bits, distinct words, the smallest fraction, and signed zero.
            decimal[] values = [new(-1, -1, -1, negative, scale),
                new(0x01234567, unchecked((int)0x89ABCDEF), 0x76543210, negative, scale),
                new(1, 0, 0, negative, scale), new(0, 0, 0, negative, scale)];

            foreach (var value in values)
            {
                DbfField field = value;

                Assert.True(field.IsType<decimal>());
                Assert.Equal(decimal.GetBits(value), decimal.GetBits(Assert.IsType<decimal>(field.Value)));
                Assert.Equal(decimal.GetBits(value), decimal.GetBits((decimal)field));
                Assert.Equal(value, (decimal?)field);
                Assert.Equal(decimal.GetBits(value), decimal.GetBits(((decimal?)field).GetValueOrDefault()));
                Assert.Equal(decimal.GetBits(value), decimal.GetBits(field.GetValue<decimal>()));
                Assert.Equal(value, field.GetValue<decimal?>());
                Assert.Equal(decimal.GetBits(value), decimal.GetBits(field.GetValue<decimal?>().GetValueOrDefault()));
            }
        }
    }

    [Fact]
    public void Conversion_WrongOrNullValueType_ThrowsInvalidCastException()
    {
        DbfField number = 42;
        DbfField empty = default;

        var wrongType = Assert.Throws<InvalidCastException>(() => (long)number);
        Assert.Contains("'Int32'", wrongType.Message);
        Assert.Contains("'Int64'", wrongType.Message);

        var nullableTarget = Assert.Throws<InvalidCastException>(() => (double?)number);
        Assert.Contains("'Int32'", nullableTarget.Message);
        Assert.Contains("'Double?'", nullableTarget.Message);

        var nullValue = Assert.Throws<InvalidCastException>(() => (int)empty);
        Assert.Contains("null DbfField", nullValue.Message);
        Assert.Contains("'Int32'", nullValue.Message);
    }

    [Fact]
    public void Match_DecimalUnionCase_ReturnsFullPrecisionValue()
    {
        DbfField field = decimal.MaxValue;

        var value = field switch
        {
            decimal number => number,
            _ => throw new InvalidOperationException("Expected the decimal case.")
        };

        Assert.Equal(decimal.MaxValue, value);
    }

    [Fact]
    public void Equals_DecimalValues_PreservesValueSemantics()
    {
        DbfField first = 123.456789M;
        DbfField equal = 123.4567890M;
        DbfField different = 123.456788M;

        Assert.Equal(first, equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, different);
        Assert.NotEqual(default, first);
    }

    [Fact]
    public void SizeOf_Union_Is24Bytes()
    {
        Assert.Equal(24, Unsafe.SizeOf<DbfField>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_Decimal_DoesNotAllocate(bool nullable)
    {
        var fields = new DbfField[128];
        var value = decimal.MaxValue;
        fields[0] = nullable ? new DbfField((decimal?)value) : new DbfField(value);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = nullable ? new DbfField((decimal?)value) : new DbfField(value);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.Equal(value, (decimal)fields[^1]);
    }

    [Fact]
    public void Access_ValueTypeCases_DoesNotAllocate()
    {
        AssertNonBoxingAccess<bool>(true, true);
        AssertNonBoxingAccess<int>(42, 42);
        AssertNonBoxingAccess<long>(long.MaxValue, long.MaxValue);
        AssertNonBoxingAccess<double>(double.NaN, double.NaN);
        AssertNonBoxingAccess<decimal>(decimal.MaxValue, decimal.MaxValue);
        var date = new DateTime(2026, 10, 9, 12, 34, 56, DateTimeKind.Utc);
        AssertNonBoxingAccess<DateTime>(date, date);
    }

    private static void AssertNonBoxingAccess<T>(DbfField field, T expected) where T : struct
    {
        _ = Access<T>(field, expected);

        var success = true;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
            success &= Access<T>(field, expected);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(success);
        Assert.Equal(0, allocated);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Access<T>(DbfField field, T expected) where T : struct =>
        field.HasValue && field.IsType<T>() && !field.IsType<T?>() && !field.IsType<object>() &&
        EqualityComparer<T>.Default.Equals(field.GetValue<T>(), expected) &&
        EqualityComparer<T?>.Default.Equals(field.GetValue<T?>(), expected) &&
        field.GetValue<Guid>() == default &&
        field.Equals(field) && field.GetHashCode() == HashCode.Combine(expected);

    [Fact]
    public void MatchAndConvert_AllCases_DoesNotAllocate()
    {
        DbfField[] fields = [true, 42, long.MaxValue, 1.23456789D, decimal.MaxValue,
            new DateTime(2026, 10, 9), "text", default];
        foreach (var field in fields)
            _ = MatchAndConvert(field);

        var success = true;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
            foreach (var field in fields)
                success &= MatchAndConvert(field);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(success);
        Assert.Equal(0, allocated);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool MatchAndConvert(DbfField field) => field switch
    {
        bool value => value.Equals((bool)field) && (bool?)field == value,
        int value => value.Equals((int)field) && (int?)field == value,
        long value => value.Equals((long)field) && (long?)field == value,
        double value => value.Equals((double)field) && Nullable.Equals((double?)field, value),
        decimal value => value.Equals((decimal)field) && (decimal?)field == value,
        DateTime value => value.Equals((DateTime)field) && (DateTime?)field == value,
        string value => value == (string?)field,
        null => !field.HasValue && (int?)field is null && field.GetValue<decimal?>() is null
    };

    [Fact]
    public void TryGetValue_AllCases_ReturnsStoredValueOrDefault()
    {
        DbfField[] fields = [false, 42, long.MaxValue, double.MaxValue, decimal.MinValue,
            new DateTime(2026, 10, 9), "", default];
        foreach (var field in fields)
        {
            Assert.Equal(!field.IsNull, field.HasValue);
            Assert.Equal(field.IsType<bool>(), field.TryGetValue(out bool boolean));
            Assert.Equal(field.GetValue<bool>(), boolean);
            Assert.Equal(field.IsType<int>(), field.TryGetValue(out int integer));
            Assert.Equal(field.GetValue<int>(), integer);
            Assert.Equal(field.IsType<long>(), field.TryGetValue(out long longInteger));
            Assert.Equal(field.GetValue<long>(), longInteger);
            Assert.Equal(field.IsType<double>(), field.TryGetValue(out double floatingPoint));
            Assert.Equal(field.GetValue<double>(), floatingPoint);
            Assert.Equal(field.IsType<decimal>(), field.TryGetValue(out decimal number));
            Assert.Equal(field.GetValue<decimal>(), number);
            Assert.Equal(field.IsType<DateTime>(), field.TryGetValue(out DateTime dateTime));
            Assert.Equal(field.GetValue<DateTime>(), dateTime);
            Assert.Equal(field.IsType<string>(), field.TryGetValue(out string? text));
            Assert.Equal(field.GetValue<string>(), text);
        }
    }

    [Fact]
    public void GetValue_ReferenceTypes_PreservesAssignability()
    {
        DbfField number = 42;
        DbfField text = "text";

        Assert.Equal(42, number.GetValue<object>());
        Assert.Equal(0, number.GetValue<IComparable>()!.CompareTo(42));
        Assert.Null(number.GetValue<string>());
        Assert.Equal("text", text.GetValue<object>());
        Assert.Null(text.GetValue<int?>());
        Assert.Equal(0, text.GetValue<int>());
    }

    [Fact]
    public void Equals_SpecialValues_PreservesValueSemantics()
    {
        Assert.Equal((DbfField)double.NaN, (DbfField)double.NaN);
        Assert.Equal((DbfField)0D, (DbfField)(-0D));
        Assert.Equal((DbfField)new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc),
            (DbfField)new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Local));
        Assert.NotEqual((DbfField)42, (DbfField)42L);
        Assert.NotEqual((DbfField)42D, (DbfField)42M);
    }

    [Fact]
    public void ToString_FormatProviders_PreservesFormatting()
    {
        DbfField[] fields = [true, 42, long.MaxValue, 1.23456789D, 123.456700M,
            new DateTime(2026, 10, 9), "text", default];
        IFormatProvider[] providers = [CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("fr-FR"), new CustomFormatter()];

        foreach (var field in fields)
            foreach (var provider in providers)
                Assert.Equal(string.Format(provider, "{0}", field.Value), field.ToString(provider));
    }

    [Fact]
    public void Constructor_ValueAndNullableInputs_PreserveUnionBehavior()
    {
        Type[] valueTypes = [typeof(bool), typeof(int), typeof(long), typeof(double), typeof(decimal), typeof(DateTime)];
        foreach (var type in valueTypes)
        {
            var constructor = typeof(DbfField).GetConstructor([type]);
            Assert.NotNull(constructor);
            Assert.Equal(type, Assert.Single(constructor.GetParameters()).ParameterType);
        }

        var decimalValue = new decimal(0, 0, 0, true, 28);
        var dateTime = new DateTime(2026, 10, 9, 12, 34, 56, DateTimeKind.Utc);
        (DbfField Direct, DbfField Nullable)[] pairs =
        [
            (new DbfField(false), new DbfField((bool?)false)),
            (new DbfField(int.MinValue), new DbfField((int?)int.MinValue)),
            (new DbfField(long.MaxValue), new DbfField((long?)long.MaxValue)),
            (new DbfField(double.NaN), new DbfField((double?)double.NaN)),
            (new DbfField(decimalValue), new DbfField((decimal?)decimalValue)),
            (new DbfField(dateTime), new DbfField((DateTime?)dateTime))
        ];

        foreach (var (direct, nullable) in pairs)
        {
            Assert.True(direct.HasValue);
            Assert.True(nullable.HasValue);
            Assert.Equal(direct, nullable);
            Assert.Equal(direct.GetHashCode(), nullable.GetHashCode());
            Assert.True(MatchAndConvert(direct));
            Assert.True(MatchAndConvert(nullable));
        }

        Assert.Equal(decimal.GetBits(decimalValue), decimal.GetBits((decimal)pairs[4].Nullable));
        Assert.Equal(DateTimeKind.Utc, ((DateTime)pairs[5].Nullable).Kind);
    }

    private sealed class CustomFormatter : IFormatProvider, ICustomFormatter
    {
        public int FormatRequests { get; private set; }

        public object? GetFormat(Type? formatType)
        {
            if (formatType != typeof(ICustomFormatter))
                return null;

            FormatRequests++;
            return this;
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider) =>
            arg is null ? "<null>" : $"custom:{arg}";
    }

    [Fact]
    public void ToString_CustomFormatter_RequestsFormatterOnce()
    {
        DbfField field = 42;
        var provider = new CustomFormatter();

        Assert.Equal("custom:42", field.ToString(provider));
        Assert.Equal(1, provider.FormatRequests);
    }
}
