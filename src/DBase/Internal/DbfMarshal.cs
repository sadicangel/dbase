using System.Buffers.Binary;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace DBase.Internal;
internal static class DbfMarshal
{
    public static DbfRecord ReadRecord(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<DbfFieldDescriptor> descriptors,
        Encoding encoding,
        char decimalSeparator,
        Memo? memo)
    {
        var status = (DbfRecordStatus)source[0];
        var fields = ImmutableArray.CreateBuilder<DbfField>(descriptors.Length);
        foreach (var descriptor in descriptors)
        {
            var field = ReadField(source.Slice(descriptor.Offset, descriptor.Length), in descriptor, encoding, decimalSeparator, memo);
            fields.Add(field);
        }

        return new DbfRecord(status, fields.MoveToImmutable());
    }
    public static void WriteRecord(
        Span<byte> target,
        ReadOnlySpan<DbfFieldDescriptor> descriptors,
        Encoding encoding,
        char decimalSeparator,
        Memo? memo,
        DbfRecordStatus status,
        params ReadOnlySpan<DbfField> fields)
    {
        target[0] = (byte)status;
        var offset = 1;
        for (var i = 0; i < fields.Length; ++i)
        {
            ref readonly var descriptor = ref descriptors[i];
            WriteField(target.Slice(descriptor.Offset, descriptor.Length), in descriptor, fields[i], encoding, decimalSeparator, memo);
            offset += descriptor.Length;
        }
    }

    public static DbfField ReadField(
        ReadOnlySpan<byte> source,
        in DbfFieldDescriptor descriptor,
        Encoding encoding,
        char decimalSeparator,
        Memo? memo)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => ReadAutoIncrement(source),
            DbfFieldType.Binary when descriptor.Length is 8 => ReadDouble(source),
            DbfFieldType.Binary => ReadMemoBinary(source, encoding, memo),
            DbfFieldType.Blob => ReadMemoBlob(source, encoding, memo),
            DbfFieldType.Character => ReadCharacter(source, encoding),
            DbfFieldType.Currency => ReadCurrency(source),
            DbfFieldType.Date => ReadDate(source, encoding),
            DbfFieldType.DateTime => ReadDateTime(source),
            DbfFieldType.Double => ReadDouble(source),
            DbfFieldType.Float => ReadNumericDouble(source, encoding, decimalSeparator),
            DbfFieldType.Int32 => ReadInt32(source),
            DbfFieldType.Logical => ReadLogical(source, encoding),
            DbfFieldType.Memo => ReadMemoString(source, encoding, memo),
            DbfFieldType.NullFlags => ReadNullFlags(source),
            DbfFieldType.Numeric when descriptor.Decimal is 0 => ReadNumericInteger(source, encoding),
            DbfFieldType.Numeric => ReadNumericDouble(source, encoding, decimalSeparator),
            DbfFieldType.Ole => ReadMemoOle(source, encoding, memo),
            DbfFieldType.Picture => ReadMemoPicture(source, encoding, memo),
            DbfFieldType.Timestamp => ReadDateTime(source),
            DbfFieldType.Variant => ReadVariant(source, encoding),
            _ => throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType)),
        };
    }
    public static void WriteField(Span<byte> target, in DbfFieldDescriptor descriptor, DbfField field, Encoding encoding, char decimalSeparator, Memo? memo)
    {
        switch (descriptor.Type)
        {
            case DbfFieldType.AutoIncrement:
                WriteAutoIncrement(target, field.GetValue<long>());
                break;
            case DbfFieldType.Binary when descriptor.Length is 8:
                WriteDouble(target, field.GetValue<double>());
                break;
            case DbfFieldType.Binary:
                WriteMemoBinary(target, field.GetValue<string>(), encoding, memo);
                break;
            case DbfFieldType.Blob:
                WriteMemoBlob(target, field.GetValue<string>(), encoding, memo);
                break;
            case DbfFieldType.Character:
                WriteCharacter(target, field.GetValue<string>(), encoding);
                break;
            case DbfFieldType.Currency:
                WriteCurrency(target, field.GetValue<decimal>());
                break;
            case DbfFieldType.Date:
                WriteDate(target, field.GetValue<DateTime?>(), encoding);
                break;
            case DbfFieldType.DateTime:
                WriteDateTime(target, field.GetValue<DateTime?>());
                break;
            case DbfFieldType.Double:
                WriteDouble(target, field.GetValue<double>());
                break;
            case DbfFieldType.Float:
                WriteNumericFloat(target, field.GetValue<double?>(), descriptor, encoding, decimalSeparator);
                break;
            case DbfFieldType.Int32:
                WriteInt32(target, field.GetValue<int>());
                break;
            case DbfFieldType.Logical:
                WriteLogical(target, field.GetValue<bool?>());
                break;
            case DbfFieldType.Memo:
                WriteMemoString(target, field.GetValue<string>(), encoding, memo);
                break;
            case DbfFieldType.NullFlags:
                WriteNullFlags(target, field.GetValue<string>());
                break;
            case DbfFieldType.Numeric when descriptor.Decimal is 0:
                WriteNumericInteger(target, field.GetValue<long?>(), encoding);
                break;
            case DbfFieldType.Numeric:
                WriteNumericFloat(target, field.GetValue<double?>(), descriptor, encoding, decimalSeparator);
                break;
            case DbfFieldType.Ole:
                WriteMemoOle(target, field.GetValue<string>(), encoding, memo);
                break;
            case DbfFieldType.Picture:
                WriteMemoPicture(target, field.GetValue<string>(), encoding, memo);
                break;
            case DbfFieldType.Timestamp:
                WriteDateTime(target, field.GetValue<DateTime?>());
                break;
            case DbfFieldType.Variant:
                WriteVariant(target, field.GetValue<string>(), encoding);
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }

    }

    public static long ReadAutoIncrement(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadInt64LittleEndian(source);
    public static void WriteAutoIncrement(Span<byte> target, long value)
        => BinaryPrimitives.WriteInt64LittleEndian(target, value);

    public static string ReadCharacter(ReadOnlySpan<byte> source, Encoding encoding)
        => encoding.GetString(source.Trim([(byte)'\0', (byte)' ']));
    public static void WriteCharacter(Span<byte> target, string? value, Encoding encoding)
    {
        target.Fill((byte)' ');
        if (value is null)
        {
            return;
        }
        _ = encoding.TryGetBytes(value, target, out _);
    }

    public static decimal ReadCurrency(ReadOnlySpan<byte> source)
        => decimal.FromOACurrency(BinaryPrimitives.ReadInt64LittleEndian(source));
    public static void WriteCurrency(Span<byte> target, decimal value)
        => BinaryPrimitives.WriteInt64LittleEndian(target, decimal.ToOACurrency(value));

    public static DateTime? ReadDate(ReadOnlySpan<byte> source, Encoding encoding)
    {
        source = source.Trim([(byte)'\0', (byte)' ']);
        if (source.Length != 8) return default;
        Span<char> date = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source[..8], date);
        return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
    }
    public static void WriteDate(Span<byte> target, DateTime? value, Encoding encoding)
    {
        if (value is null)
        {
            target.Fill((byte)' ');
            return;
        }

        var dateTime = value.Value;
        Span<char> chars = stackalloc char[8];
        if (!dateTime.TryFormat(chars, out _, "yyyyMMdd", CultureInfo.InvariantCulture) ||
            !encoding.TryGetBytes(chars, target, out _))
        {
            target.Fill((byte)' ');
        }
    }

    public static DateTime? ReadDateTime(ReadOnlySpan<byte> source)
    {
        var julian = BinaryPrimitives.ReadInt32LittleEndian(source);
        if (julian is 0) return default;
        var milliseconds = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4, 4));
        return DateTime.FromOADate(julian - 2415018.5).AddMilliseconds(milliseconds);
    }
    public static void WriteDateTime(Span<byte> target, DateTime? value)
    {
        if (value is null)
        {
            target.Clear();
            return;
        }

        var dateTime = value.Value;
        var julian = (int)(dateTime.Date.ToOADate() + 2415018.5);
        BinaryPrimitives.WriteInt32LittleEndian(target, julian);
        var milliseconds = 43200000 /* 12h */ + (int)dateTime.TimeOfDay.TotalMilliseconds;
        BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4, 4), milliseconds);
    }

    public static double ReadDouble(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadDoubleLittleEndian(source);
    public static void WriteDouble(Span<byte> target, double value)
        => BinaryPrimitives.WriteDoubleLittleEndian(target, value);

    public static int ReadInt32(ReadOnlySpan<byte> source)
        => BinaryPrimitives.ReadInt32LittleEndian(source);
    public static void WriteInt32(Span<byte> target, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(target, value);

    public static bool? ReadLogical(ReadOnlySpan<byte> source, Encoding encoding)
    {
        if (encoding.GetCharCount(source) is not 1) return default;
        Span<char> v = ['\0'];
        encoding.GetChars(source, v);
        return char.ToUpperInvariant(v[0]) switch
        {
            '?' or ' ' => null,
            'T' or 'Y' or '1' => true,
            'F' or 'N' or '0' => false,
            // TODO: Maybe we should just return null?
            _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
        };
    }
    public static void WriteLogical(Span<byte> target, bool? value)
        => target[0] = value is null ? (byte)'?' : value.Value ? (byte)'T' : (byte)'F';

    private static string ReadMemo(ReadOnlySpan<byte> source, MemoRecordType type, Encoding encoding, Memo? memo)
    {
        if (memo is null || source is [])
        {
            return string.Empty;
        }

        var index = 0;
        if (source.Length is 4)
        {
            index = BinaryPrimitives.ReadInt32LittleEndian(source);
        }
        else
        {
            Span<char> chars = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, chars);
            chars = chars.Trim();
            if (chars is [])
            {
                return string.Empty;
            }
            index = int.Parse(chars);
        }

        if (index == 0)
        {
            return string.Empty;
        }

        var data = type is MemoRecordType.Memo
            ? encoding.GetString(memo[index].Span)
            : Convert.ToBase64String(memo[index].Span);

        return data;
    }
    private static void WriteMemo(Span<byte> target, MemoRecordType type, string? value, Encoding encoding, Memo? memo)
    {
        target.Fill(target.Length is 4 ? (byte)0 : (byte)' ');
        if (memo is null || string.IsNullOrEmpty(value))
        {
            return;
        }

        var index = memo.NextIndex;

        if (target.Length is 4)
        {
            BinaryPrimitives.WriteInt32LittleEndian(target, index);
        }
        else
        {
            Span<char> chars = stackalloc char[10];
            index.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
            var bytesRequired = encoding.GetByteCount(chars[..charsWritten]);
            encoding.TryGetBytes(chars[..charsWritten], target[Math.Max(0, 10 - bytesRequired)..], out _);
        }

        var data = type is MemoRecordType.Memo
            ? encoding.GetBytes(value)
            : Convert.FromBase64String(value);
        memo.Add(new MemoRecord(type, data));
    }

    public static string? ReadMemoString(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
        => ReadMemo(source, MemoRecordType.Memo, encoding, memo);
    public static void WriteMemoString(Span<byte> target, string? value, Encoding encoding, Memo? memo)
        => WriteMemo(target, MemoRecordType.Memo, value, encoding, memo);

    public static string? ReadMemoBinary(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
        => ReadMemo(source, MemoRecordType.Object, encoding, memo);
    public static void WriteMemoBinary(Span<byte> target, string? value, Encoding encoding, Memo? memo)
        => WriteMemo(target, MemoRecordType.Object, value, encoding, memo);

    public static string? ReadMemoBlob(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
        => ReadMemo(source, MemoRecordType.Object, encoding, memo);
    public static void WriteMemoBlob(Span<byte> target, string? value, Encoding encoding, Memo? memo)
        => WriteMemo(target, MemoRecordType.Object, value, encoding, memo);

    public static string? ReadMemoOle(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
        => ReadMemo(source, MemoRecordType.Object, encoding, memo);
    public static void WriteMemoOle(Span<byte> target, string? value, Encoding encoding, Memo? memo)
        => WriteMemo(target, MemoRecordType.Object, value, encoding, memo);

    public static string? ReadMemoPicture(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
        => ReadMemo(source, MemoRecordType.Picture, encoding, memo);
    public static void WriteMemoPicture(Span<byte> target, string? value, Encoding encoding, Memo? memo)
        => WriteMemo(target, MemoRecordType.Picture, value, encoding, memo);

    public static string ReadNullFlags(ReadOnlySpan<byte> source)
        => Convert.ToHexString(source);
    public static void WriteNullFlags(Span<byte> target, string? value)
    {
        if (value is null)
        {
            target.Clear();
            return;
        }

        Convert.FromHexString(value, target, out _, out _);
    }

    public static double? ReadNumericDouble(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator)
    {
        source = source.Trim([(byte)'\0', (byte)' ']);
        if (source.IsEmpty || (source.Length == 1 && !char.IsAsciiDigit((char)source[0])))
            return default;
        Span<char> @double = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, @double);
        if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
            @double[idx] = '.';
        return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);
    }
    public static void WriteNumericFloat(Span<byte> target, double? value, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator)
    {
        target.Fill((byte)' ');
        if (value is null)
        {
            return;
        }

        var f64 = value.Value;

        Span<char> format = ['F', '\0', '\0'];
        if (!descriptor.Decimal.TryFormat(format[1..], out var charsWritten))
            throw new InvalidOperationException($"Failed to create decimal format");
        format = format[..(1 + charsWritten)];

        Span<char> chars = stackalloc char[20];
        if (!f64.TryFormat(chars, out charsWritten, format, CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Failed to format value '{f64}' for field type '{descriptor.Type}'");

        if (decimalSeparator is not '.' && chars.IndexOf(decimalSeparator) is var idx and >= 0)
            chars[idx] = decimalSeparator;

        _ = encoding.TryGetBytes(chars[..charsWritten], target, out _);
    }

    public static long? ReadNumericInteger(ReadOnlySpan<byte> source, Encoding encoding)
    {
        source = source.Trim([(byte)'\0', (byte)' ']);
        if (source.IsEmpty) return default;
        Span<char> integer = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, integer);
        return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
    public static void WriteNumericInteger(Span<byte> target, long? value, Encoding encoding)
    {
        target.Fill((byte)' ');
        if (value is null)
        {
            return;
        }

        var i64 = value.Value;

        Span<char> @long = stackalloc char[20];
        if (!i64.TryFormat(@long, out var charsWritten, "D", CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"Failed to format value '{i64}' as '{DbfFieldType.Numeric}'");
        _ = encoding.TryGetBytes(@long[..charsWritten], target, out _);
    }

    public static string ReadVariant(ReadOnlySpan<byte> source, Encoding encoding)
        => encoding.GetString(source[..source[^1]]);
    public static void WriteVariant(Span<byte> target, string? value, Encoding encoding)
    {
        if (value is null)
        {
            target.Fill((byte)' ');
            return;
        }
        _ = encoding.TryGetBytes(value, target[..^1], out var bytesWritten);
        target[^1] = (byte)bytesWritten;
    }
}
