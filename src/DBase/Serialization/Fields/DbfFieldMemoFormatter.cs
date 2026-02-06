using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DotNext.Buffers;
using DotNext.Buffers.Text;
using DotNext.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldMemoFormatter
{
    private static string ReadMemo(ReadOnlySpan<byte> source, MemoRecordType type, Encoding encoding, Memo? memo)
    {
        if (memo is null || source is [])
            return string.Empty;

        int index;
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
                return string.Empty;
            index = int.Parse(chars);
        }

        if (index == 0)
            return string.Empty;

        var writer = new BufferWriterSlim<byte>(memo.BlockLength);

        try
        {
            memo.Get(index, out _, ref writer);

            var data = type is MemoRecordType.Memo
                ? encoding.GetString(writer.WrittenSpan)
                : Convert.ToBase64String(writer.WrittenSpan);

            return data;
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static void WriteMemo(Span<byte> target, MemoRecordType type, ReadOnlySpan<char> value, Encoding encoding, Memo? memo)
    {
        target.Fill(target.Length is 4 ? (byte)0 : (byte)' ');
        if (memo is null || value.Length is 0)
            return;

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

        using var data = type is MemoRecordType.Memo
            ? encoding.GetBytes(value)
            : new Base64Decoder().DecodeFromUtf16(value);

        memo.Add(type, data.Span);
    }

    public static DbfFieldFormatter Create(Type propertyType, MemoRecordType recordType)
    {
        if (propertyType == typeof(DbfField))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                (DbfField)ReadMemo(source, recordType, context.Encoding, context.Memo);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, ((DbfField)value!).GetValue<string>(), context.Encoding, context.Memo);
        }

        if (propertyType == typeof(string))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, (string?)value, context.Encoding, context.Memo);
        }

        if (propertyType == typeof(char[]))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo).ToCharArray();

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, (char[]?)value, context.Encoding, context.Memo);
        }

        if (propertyType == typeof(ReadOnlyMemory<char>))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemo(source, recordType, context.Encoding, context.Memo).AsMemory();

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemo(target, recordType, ((ReadOnlyMemory<char>)value!).Span, context.Encoding, context.Memo);
        }

        throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(propertyType));
    }
}
