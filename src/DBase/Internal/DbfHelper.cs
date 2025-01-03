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

        return version.GetVersionNumber() switch
        {
            < 3 => ReadHeader02(dbf),
            < 7 => ReadHeader03(dbf),
            _ =>
                throw new NotSupportedException($"Unsupported DBF version '0x{(byte)version:X2}'"),
        };

        static DbfHeader ReadHeader02(Stream dbf)
        {
            Unsafe.SkipInit(out DbfHeader02 header);
            dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
            return header;
        }

        static DbfHeader ReadHeader03(Stream dbf)
        {
            Unsafe.SkipInit(out DbfHeader header);
            dbf.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1)));
            return header;
        }
    }

    public static void WriteHeader(Stream dbf, in DbfHeader header, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        dbf.Position = 0;
        switch (header.Version.GetVersionNumber())
        {
            case < 3:
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
                break;

            case < 7:
                dbf.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in header, 1)));
                dbf.Write(MemoryMarshal.AsBytes(descriptors.AsSpan()));
                dbf.WriteByte(0x0D);
                break;

            default:
                throw new InvalidDataException($"Invalid DBF version '0x{(byte)header.Version:X2}'");
        };
    }

    public static ImmutableArray<DbfFieldDescriptor> ReadDescriptors(Stream dbf, in DbfHeader header)
    {
        return header.Version.GetVersionNumber() switch
        {
            < 3 => ReadDescriptors02(dbf, in header),
            < 7 => ReadDescriptors03(dbf, in header),
            _ => throw new NotSupportedException($"Unsupported DBF version '0x{(byte)header.Version:X2}'"),
        };

        static ImmutableArray<DbfFieldDescriptor> ReadDescriptors02(Stream dbf, in DbfHeader header)
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

            var descriptors03 = ImmutableArray.CreateBuilder<DbfFieldDescriptor>(count);
            for (var i = 0; i < count; ++i)
                descriptors03.Add(descriptors02[i]);

            ArrayPool<DbfFieldDescriptor02>.Shared.Return(descriptors02);

            return descriptors03.MoveToImmutable();
        }

        static ImmutableArray<DbfFieldDescriptor> ReadDescriptors03(Stream dbf, in DbfHeader header)
        {
            dbf.Position = DbfHeader.Size;

            var descriptors03 = new DbfFieldDescriptor[(header.HeaderLength - DbfHeader.Size) / DbfFieldDescriptor.Size];
            dbf.ReadExactly(MemoryMarshal.AsBytes(descriptors03.AsSpan()));

            if (dbf.ReadByte() != 0x0D)
                throw new InvalidDataException("Invalid DBF header terminator");

            return ImmutableCollectionsMarshal.AsImmutableArray(descriptors03);
        }
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
        switch (descriptor.Type)
        {
            case DbfFieldType.Character:
                source = source.Trim([(byte)'\0', (byte)' ']);
                return source.Length > 0 ? encoding.GetString(source) : string.Empty;

            case DbfFieldType.Numeric when descriptor.Decimal == 0:
                source = source.Trim([(byte)'\0', (byte)' ']);
                if (source is [])
                    return default(long?);
                Span<char> integer = stackalloc char[source.Length];
                encoding.GetChars(source, integer);
                return long.Parse(integer, NumberStyles.Integer, CultureInfo.InvariantCulture);

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                source = source.Trim([(byte)'\0', (byte)' ']);
                if (source is [] || source is [var b] && !char.IsAsciiDigit((char)b))
                    return default(double?);
                Span<char> @double = stackalloc char[source.Length];
                encoding.GetChars(source, @double);
                if (decimalSeparator != '.' && @double.IndexOf(decimalSeparator) is var idx and >= 0)
                    @double[idx] = '.';
                return double.Parse(@double, NumberStyles.Number, CultureInfo.InvariantCulture);

            case DbfFieldType.Int32:
            case DbfFieldType.AutoIncrement:
                return MemoryMarshal.Read<int>(source);

            case DbfFieldType.Double:
                return MemoryMarshal.Read<double>(source);

            case DbfFieldType.Date:
                Span<char> date = stackalloc char[8];
                encoding.GetChars(source[..8], date);
                return DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);

            case DbfFieldType.Timestamp:
                return DateTime.FromOADate(MemoryMarshal.Read<int>(source[..4]) - 2415018.5) + TimeSpan.FromMilliseconds(MemoryMarshal.Read<int>(source.Slice(4, 4)));

            case DbfFieldType.Logical:
                var l = '\0';
                encoding.GetChars(source[..1], MemoryMarshal.CreateSpan(ref l, 1));
                return char.ToUpperInvariant(l) switch
                {
                    '?' or ' ' => default(bool?),
                    'T' or 'Y' or '1' => true,
                    'F' or 'N' or '0' => false,
                    _ => throw new InvalidOperationException($"Invalid {nameof(DbfFieldType.Logical)} value '{encoding.GetString(source)}'"),
                };

            case DbfFieldType.Ole:
            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
                // TODO: Actually read data from the memo file using this value as the memo index.
                return encoding.GetString(source);

            default:
                throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
        }
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
        }
        else
        {
            switch (descriptor.Type)
            {
                case DbfFieldType.Character:
                    {
                        // Left align.
                        encoding.GetBytes(field.GetValue<string>().AsSpan(), target);
                    }
                    break;

                case DbfFieldType.Numeric when descriptor.Decimal == 0:
                    {
                        Span<byte> temp = stackalloc byte[descriptor.Length];
                        if (!field.GetValue<long>().TryFormat(temp, out _, "D", CultureInfo.InvariantCulture))
                            throw new InvalidOperationException($"Failed to format value '{field.Value}' as '{descriptor.Type}'");

                        // Right align.
                        var padding = descriptor.Length - temp.Length;
                        temp.CopyTo(target[padding..]);
                    }
                    break;

                case DbfFieldType.Numeric:
                case DbfFieldType.Float:
                    {
                        Span<char> format = ['F', '\0', '\0'];
                        if (!descriptor.Decimal.TryFormat(format[1..], out var charsWritten))
                            throw new InvalidOperationException($"Failed to create decimal format");
                        format = format.Slice(1, charsWritten);

                        Span<byte> temp = stackalloc byte[descriptor.Length];
                        if (!field.GetValue<double>().TryFormat(temp, out _, format, CultureInfo.InvariantCulture))
                            throw new InvalidOperationException($"Failed to format value '{field.Value}' as '{descriptor.Type}'");

                        Span<byte> decimalSeparatorByte = [0];
                        encoding.GetBytes([decimalSeparator], decimalSeparatorByte);
                        temp.Replace((byte)'.', decimalSeparatorByte[0]);

                        // Right align.
                        var padding = descriptor.Length - temp.Length;
                        temp.CopyTo(target[padding..]);
                    }
                    break;

                case DbfFieldType.Int32:
                case DbfFieldType.AutoIncrement:
                    {
                        var i32 = (int)field.GetValue<long>();
                        MemoryMarshal.Write(target, in i32);
                    }
                    break;

                case DbfFieldType.Double:
                    {
                        var f64 = field.GetValue<double>();
                        MemoryMarshal.Write(target, in f64);
                    }
                    break;

                case DbfFieldType.Date:
                    {
                        var date = field.GetValue<DateTime>();
                        for (int i = 0, y = date.Year; i < 4; ++i, y /= 10)
                            target[i] = (byte)(y % 10 + '0');
                        for (int i = 4, m = date.Month; i < 6; ++i, m /= 10)
                            target[i] = (byte)((m & 10) + '0');
                        for (int i = 6, d = date.Day; i < 8; ++i, d /= 10)
                            target[i] = (byte)((d & 10) + '0');
                    }
                    break;

                case DbfFieldType.Timestamp:
                    {
                        var timestamp = field.GetValue<DateTime>();
                        var julian = (int)(timestamp.Date.ToOADate() + 2415018.5);
                        MemoryMarshal.Write(target[..4], in julian);
                        var milliseconds = (int)timestamp.TimeOfDay.TotalMilliseconds;
                        MemoryMarshal.Write(target[4..], in milliseconds);
                    }
                    break;

                case DbfFieldType.Logical:
                    {
                        target[0] = (byte)(field.GetValue<bool>() ? 'T' : 'F');
                    }
                    break;

                case DbfFieldType.Memo:
                case DbfFieldType.Binary:
                case DbfFieldType.Ole:
                    {
                        encoding.GetBytes(field.GetValue<string>(), target);
                    }
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
            }
        }
    }
}
