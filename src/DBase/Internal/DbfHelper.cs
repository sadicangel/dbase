using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DBase.Internal;

internal static class DbfHelper
{
    public static DbfVersion GetVersionFromDescriptors(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var version = DbfVersion.DBase03;
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type == DbfFieldType.Memo)
                version = DbfVersion.DBase83;
        }
        return version;
    }

    public static DbfTableFlags GetTableFlagsFromDescriptors(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var flags = DbfTableFlags.None;
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type == DbfFieldType.Memo)
                flags |= DbfTableFlags.HasMemoField;
        }
        return flags;
    }

    public static DbfHeader ReadHeader(Stream dbf)
    {
        dbf.Position = 0;
        var version = (DbfVersion)dbf.ReadByte();
        dbf.Position = 0;

        if (version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'");
        }

        if (version is DbfVersion.DBase02)
        {
            Unsafe.SkipInit(out DbfHeader02 header02);
            dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header02, 1)));
            return header02;
        }

        Unsafe.SkipInit(out DbfHeader header);
        dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
        return header;
    }

    public static void WriteHeader(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        if (header.Version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        dbf.Position = 0;

        if (header.Version is DbfVersion.DBase02)
        {
            DbfHeader02 header02 = header;
            dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header02, 1)));
            foreach (var descriptor in descriptors)
            {
                DbfFieldDescriptor02 descriptor02 = descriptor;
                dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref descriptor02, 1)));
            }
            dbf.WriteByte(0x0D);
            if (dbf.Position is not DbfHeader02.HeaderLengthInDisk)
            {
                dbf.Position = DbfHeader02.HeaderLengthInDisk - 1;
                dbf.WriteByte(0x00);
            }
            return;
        }

        dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in header, 1)));
        dbf.Write(MemoryMarshal.AsBytes(descriptors.AsSpan()));
        dbf.WriteByte(0x0D);
    }

    public static ImmutableArray<DbfFieldDescriptor> ReadDescriptors(Stream dbf, in DbfHeader header)
    {
        if (header.Version.GetVersionNumber() > 7)
        {
            throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'");
        }

        if (header.Version is DbfVersion.DBase02)
        {
            dbf.Position = DbfHeader02.Size;

            var descriptors02 = ArrayPool<DbfFieldDescriptor02>.Shared.Rent(32);
            dbf.ReadExactly(MemoryMarshal.AsBytes(descriptors02.AsSpan(0, 32)));

            var count = 32;
            if (dbf.ReadByte() is not 0x0D)
            {
                count = Array.FindIndex(descriptors02, static descriptor => descriptor.Name[0] is 0x0D);
                if (count < 0) count = 32; // Invalid terminator, assume all 32 fields.
            }

            var descriptors = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(count);
            for (var i = 0; i < count; ++i)
                descriptors.Add(descriptors02[i]);

            ArrayPool<DbfFieldDescriptor02>.Shared.Return(descriptors02);

            return descriptors.MoveToImmutable();
        }


        dbf.Position = DbfHeader.Size;

        var descriptors03 = new DbfFieldDescriptor[(header.HeaderLength - DbfHeader.Size) / DbfFieldDescriptor.Size];
        dbf.ReadExactly(MemoryMarshal.AsBytes(descriptors03.AsSpan()));

        if (dbf.ReadByte() is not 0x0D)
        {
            if (!header.Version.IsFoxPro())
                throw new InvalidDataException("Invalid DBF header terminator");

            // TODO: Move this to a separate method.

            // FoxPro DBF files can have extra metadata after the field descriptors.
            // We need to find the terminator byte to know where the field descriptors end.
            var count = Array.FindIndex(descriptors03, static descriptor => descriptor.Name[0] is 0x0D);
            if (count < 0)
                throw new InvalidDataException("Invalid DBF header terminator");
            descriptors03 = descriptors03[..count];

            // TODO: Read FoxPro metadata.
            //dbf.Position = DbfHeader.Size + DbfFieldDescriptor.Size * count + 1 /* terminator byte */;
            //var codePage = dbf.ReadByte();
            //dbf.ReadExactly([0, 0]); // Reserved bytes.
            //var blockSize = 0;
            //if (header.TableFlags.HasFlag(DbfTableFlags.HasMemoField))
            //{
            //    dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref blockSize, 1)));
            //}
            //if (descriptors03.Any(d => d.Flags.HasFlag(DbfFieldFlags.Nullable)))
            //{
            //    dbf.ReadExactly(stackalloc byte[(count + 7) / 8]);
            //}
            //foreach (var descriptor in descriptors03.Where(d => d.Flags.HasFlag(DbfFieldFlags.AutoIncrement)))
            //{
            //    var startValue = 0;
            //    var stepValue = 0;
            //    dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref startValue, 1)));
            //    dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref stepValue, 1)));
            //}
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(descriptors03);
    }

    public static DbfRecord ReadRecord(ReadOnlySpan<byte> source, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator)
    {
        var fields = new DbfField[descriptors.Length];
        var status = (DbfRecordStatus)source[0];
        var offset = 1;
        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = descriptors[i];
            var field = ReadField(source.Slice(offset, descriptor.Length), in descriptor, encoding, decimalSeparator);
            fields[i] = field;
            offset += descriptor.Length;
        }

        return new DbfRecord(status, ImmutableCollectionsMarshal.AsImmutableArray(fields));
    }

    public static DbfField ReadField(ReadOnlySpan<byte> source, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator)
    {
        return descriptor.Type switch
        {
            DbfFieldType.Character => ReadCharacterField(source, encoding),
            DbfFieldType.Numeric when descriptor.Decimal == 0 => ReadNumericField(source, encoding),
            DbfFieldType.Numeric or DbfFieldType.Float => ReadFloatField(source, encoding, decimalSeparator),
            DbfFieldType.Int32 => ReadInt32Field(source),
            DbfFieldType.Double => ReadDoubleField(source),
            DbfFieldType.Date => ReadDateField(source, encoding),
            DbfFieldType.AutoIncrement => ReadAutoIncrementField(source),
            DbfFieldType.Timestamp => ReadTimestampField(source),
            DbfFieldType.DateTime => ReadDateTimeField(source),
            DbfFieldType.Logical => ReadLogicalField(source, encoding),
            DbfFieldType.Memo when descriptor.Length != 4 => ReadMemoField(source, encoding),
            DbfFieldType.Memo or DbfFieldType.Binary or DbfFieldType.Blob or DbfFieldType.Ole => ReadBinaryField(source),
            _ => throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType)),
        };

        static DbfField ReadCharacterField(ReadOnlySpan<byte> source, Encoding encoding) =>
            encoding.GetString(source.Trim([(byte)'\0', (byte)' ']));

        static DbfField ReadNumericField(ReadOnlySpan<byte> source, Encoding encoding)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.IsEmpty)
                return default(long?);
            Span<char> integer = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, integer);
            return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        static DbfField ReadFloatField(ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.IsEmpty || (source.Length == 1 && !char.IsAsciiDigit((char)source[0])))
                return default(double?);
            Span<char> @double = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source, @double);
            if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
                @double[idx] = '.';
            return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        static DbfField ReadInt32Field(ReadOnlySpan<byte> source) => MemoryMarshal.Read<int>(source);

        static DbfField ReadDoubleField(ReadOnlySpan<byte> source) => MemoryMarshal.Read<double>(source);

        static DbfField ReadDateField(ReadOnlySpan<byte> source, Encoding encoding)
        {
            source = source.Trim([(byte)'\0', (byte)' ']);
            if (source.Length != 8)
                return default(DateTime?);
            Span<char> date = stackalloc char[encoding.GetCharCount(source)];
            encoding.GetChars(source[..8], date);
            return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // TODO: Is this correct?
        static DbfField ReadTimestampField(ReadOnlySpan<byte> source)
        {
            var julian = MemoryMarshal.Read<int>(source[..4]);
            if (julian is 0) return default(DateTime?);
            var milliseconds = MemoryMarshal.Read<int>(source.Slice(4, 4));
            return DateTime.FromOADate(julian - 2415018.5).AddMilliseconds(milliseconds);
        }

        static DbfField ReadDateTimeField(ReadOnlySpan<byte> source)
        {
            var julian = MemoryMarshal.Read<int>(source[..4]);
            if (julian is 0) return default(DateTime?);
            var milliseconds = MemoryMarshal.Read<int>(source.Slice(4, 4));
            return DateTime.FromOADate(julian - 2415018.5).AddMilliseconds(milliseconds);
        }

        static DbfField ReadLogicalField(ReadOnlySpan<byte> source, Encoding encoding)
        {
            var l = '\0';
            encoding.GetChars(source[..1], MemoryMarshal.CreateSpan(ref l, 1));
            return char.ToUpperInvariant(l) switch
            {
                '?' or ' ' => default(bool?),
                'T' or 'Y' or '1' => true,
                'F' or 'N' or '0' => false,
                _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
            };
        }

        static DbfField ReadAutoIncrementField(ReadOnlySpan<byte> source) =>
            MemoryMarshal.Read<long>(source);

        static DbfField ReadMemoField(ReadOnlySpan<byte> source, Encoding encoding) =>
            encoding.GetString(source);

        static DbfField ReadBinaryField(ReadOnlySpan<byte> source) =>
            MemoryMarshal.Read<int>(source);
    }

    public static void WriteRecord(DbfRecord record, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Span<byte> target)
    {
        target[0] = (byte)record.Status;
        var offset = 1;
        for (var i = 0; i < descriptors.Length; ++i)
        {
            WriteField(record[i], in descriptors.ItemRef(i), encoding, decimalSeparator, target.Slice(offset, descriptors[i].Length));
            offset += descriptors[i].Length;
        }
    }

    public static void WriteField(DbfField field, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Span<byte> target)
    {
        target.Fill((byte)' ');
        if (field.IsNull)
        {
            if (descriptor.Type is DbfFieldType.Logical)
                target[0] = (byte)'?';

            return;
        }

        switch (descriptor.Type)
        {
            case DbfFieldType.Character:
                WriteString(field, encoding, target);
                break;

            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                WriteNumericField(field, encoding, target);
                break;

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                WriteFloatField(field, descriptor, encoding, decimalSeparator, target);
                break;

            case DbfFieldType.Int32:
                WriteInt32Field(field, target);
                break;

            case DbfFieldType.Double:
                WriteDoubleField(field, target);
                break;

            case DbfFieldType.Date:
                WriteDateField(field, target);
                break;

            case DbfFieldType.Timestamp:
                WriteTimestampField(field, target);
                break;

            case DbfFieldType.DateTime:
                WriteDateTimeField(field, target);
                break;

            case DbfFieldType.Logical:
                WriteLogicalField(field, target);
                break;

            case DbfFieldType.AutoIncrement:
                WriteAutoIncrementField(field, target);
                break;

            case DbfFieldType.Memo when descriptor.Length != 4:
                WriteString(field, encoding, target);
                break;

            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
            case DbfFieldType.Ole:
                WriteBinaryField(field, target);
                break;

            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }

        static void WriteString(DbfField field, Encoding encoding, Span<byte> target)
        {
            var @string = field.GetValue<string>().AsSpan();
            if (@string.Length == 0) return;

            // Trim the binary data to fit the target length.
            // TODO: Make this more efficient.
            int bytesRequired;
            while ((bytesRequired = encoding.GetByteCount(@string)) > target.Length)
            {
                @string = @string[..^1];
            }
            encoding.GetBytes(@string, target);
        }

        static void WriteNumericField(DbfField field, Encoding encoding, Span<byte> target)
        {
            Span<byte> temp = stackalloc byte[target.Length];
            if (!field.GetValue<long>().TryFormat(temp, out _, "D", CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Failed to format value '{field.Value}' as '{DbfFieldType.Numeric}'");

            // Right align.
            var padding = target.Length - temp.Length;
            temp.CopyTo(target[padding..]);
        }

        static void WriteFloatField(DbfField field, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Span<byte> target)
        {
            Span<char> format = ['F', '\0', '\0'];
            if (!descriptor.Decimal.TryFormat(format[1..], out var charsWritten))
                throw new InvalidOperationException($"Failed to create decimal format");
            format = format[..(1 + charsWritten)];

            Span<byte> temp = stackalloc byte[target.Length];
            if (!field.GetValue<double>().TryFormat(temp, out _, format, CultureInfo.InvariantCulture))
                throw new InvalidOperationException($"Failed to format value '{field.Value}' as '{DbfFieldType.Numeric}'");

            Span<byte> decimalSeparatorByte = stackalloc byte[1];
            encoding.GetBytes([decimalSeparator], decimalSeparatorByte);
            temp.Replace((byte)'.', decimalSeparatorByte[0]);

            // Right align.
            var padding = target.Length - temp.Length;
            temp.CopyTo(target[padding..]);
        }

        static void WriteInt32Field(DbfField field, Span<byte> target)
        {
            var i32 = (int)field.GetValue<long>();
            MemoryMarshal.Write(target, in i32);
        }

        static void WriteDoubleField(DbfField field, Span<byte> target)
        {
            var f64 = field.GetValue<double>();
            MemoryMarshal.Write(target, in f64);
        }

        static void WriteDateField(DbfField field, Span<byte> target)
        {
            var date = field.GetValue<DateTime>();
            for (int i = 0, y = date.Year; i < 4; ++i, y /= 10)
                target[i] = (byte)(y % 10 + '0');
            for (int i = 4, m = date.Month; i < 6; ++i, m /= 10)
                target[i] = (byte)((m & 10) + '0');
            for (int i = 6, d = date.Day; i < 8; ++i, d /= 10)
                target[i] = (byte)((d & 10) + '0');
        }

        static void WriteTimestampField(DbfField field, Span<byte> target)
        {
            var timestamp = field.GetValue<DateTime>();
            var julian = (int)(timestamp.Date.ToOADate() + 2415018.5);
            MemoryMarshal.Write(target[..4], in julian);
            var milliseconds = (int)timestamp.TimeOfDay.TotalMilliseconds;
            MemoryMarshal.Write(target[4..], in milliseconds);
        }

        static void WriteDateTimeField(DbfField field, Span<byte> target)
        {
            var dateTime = field.GetValue<DateTime>();
            var julian = (int)(dateTime.Date.ToOADate() + 2415018.5);
            MemoryMarshal.Write(target[..4], in julian);
            var milliseconds = (int)dateTime.TimeOfDay.TotalMilliseconds;
            MemoryMarshal.Write(target[4..], in milliseconds);
        }

        static void WriteLogicalField(DbfField field, Span<byte> target) =>
            target[0] = (byte)(field.GetValue<bool>() ? 'T' : 'F');

        static void WriteAutoIncrementField(DbfField field, Span<byte> target)
        {
            var i64 = field.GetValue<long>();
            MemoryMarshal.Write(target, in i64);
        }

        static void WriteBinaryField(DbfField field, Span<byte> target)
        {
            var i32 = field.GetValue<int>();
            MemoryMarshal.Write(target, in i32);
        }
    }
}
