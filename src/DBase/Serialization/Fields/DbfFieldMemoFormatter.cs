using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using DotNext.Buffers;
using DotNext.Buffers.Text;
using DotNext.Text;

namespace DBase.Serialization.Fields;

internal static class DbfFieldMemoFormatter
{
    private static int ReadMemoIndex(ReadOnlySpan<byte> source, Encoding encoding)
    {
        if (source.Length is 4)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(source);
        }

        Span<char> chars = stackalloc char[encoding.GetCharCount(source)];
        encoding.GetChars(source, chars);
        chars = chars.Trim();
        return chars is [] ? 0 : int.Parse(chars);
    }

    private static ReadOnlySpan<byte> ReadMemoData(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo, ref BufferWriterSlim<byte> writer)
    {
        if (memo is null || source is [])
            return [];

        var index = ReadMemoIndex(source, encoding);
        if (index == 0)
            return [];

        memo.Get(index, out _, ref writer);
        return writer.WrittenSpan;
    }

    private static string ReadMemo(ReadOnlySpan<byte> source, MemoRecordType type, Encoding encoding, Memo? memo)
    {
        if (memo is null || source is [])
            return string.Empty;

        var writer = new BufferWriterSlim<byte>(memo.BlockLength);

        try
        {
            var data = ReadMemoData(source, encoding, memo, ref writer);
            return type is MemoRecordType.Memo
                ? encoding.GetString(data)
                : Convert.ToBase64String(data);
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static byte[] ReadMemoBytes(ReadOnlySpan<byte> source, Encoding encoding, Memo? memo)
    {
        if (memo is null || source is [])
            return [];

        var writer = new BufferWriterSlim<byte>(memo.BlockLength);

        try
        {
            return ReadMemoData(source, encoding, memo, ref writer).ToArray();
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
            WriteTextMemoIndex(target, chars[..charsWritten], encoding);
        }

        using var data = type is MemoRecordType.Memo
            ? encoding.GetBytes(value)
            : new Base64Decoder().DecodeFromUtf16(value);

        memo.Add(type, data.Span);
    }

    private static void WriteMemoBytes(Span<byte> target, MemoRecordType type, byte[]? value, Encoding encoding, Memo? memo)
    {
        target.Fill(target.Length is 4 ? (byte)0 : (byte)' ');
        if (memo is null || value is null || value.Length is 0)
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
            WriteTextMemoIndex(target, chars[..charsWritten], encoding);
        }

        memo.Add(type, value);
    }

    private static void WriteTextMemoIndex(Span<byte> target, ReadOnlySpan<char> value, Encoding encoding)
    {
        var bytesRequired = encoding.GetByteCount(value);
        if (bytesRequired > target.Length)
        {
            throw new OverflowException($"Memo index requires {bytesRequired} bytes, but the field length is {target.Length} bytes.");
        }

        var written = DbfTextFieldFormatter.WriteTruncated(target[(target.Length - bytesRequired)..], value, encoding);
        if (written != bytesRequired)
        {
            throw new InvalidOperationException("Memo index could not be encoded completely.");
        }
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

        if (propertyType == typeof(byte[]))
        {
            return new DbfFieldFormatter(Read, Write);

            object? Read(ReadOnlySpan<byte> source, DbfSerializationContext context) =>
                ReadMemoBytes(source, context.Encoding, context.Memo);

            void Write(Span<byte> target, object? value, DbfSerializationContext context) =>
                WriteMemoBytes(target, recordType, (byte[]?)value, context.Encoding, context.Memo);
        }

        throw new ArgumentException("Memo fields must be of a type convertible to string", nameof(propertyType));
    }
}
