using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using DBase.Internal;

namespace DBase;

internal sealed class DbfWriter : IDisposable
{
    private readonly Stream _stream;
    private DbfHeader _header;

    public ref readonly DbfHeader Header => ref _header;
    public ImmutableArray<DbfFieldDescriptor> Descriptors { get; }
    public Encoding Encoding { get; }
    public char DecimalSeparator { get; }

    public uint RecordCount
    {
        get => _header.RecordCount;
        private set
        {
            var header = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _header, 1));
            ref var recordCount = ref MemoryMarshal.AsRef<uint>(
                header[..Marshal.OffsetOf<DbfHeader>(nameof(DbfHeader.RecordCount)).ToInt32()]);
            recordCount = value;
            UpdateLastUpdate();
        }
    }

    public DbfWriter(Stream stream, ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfZero(descriptors.Length);

        _stream = stream;
        _header = new DbfHeader
        {
            HeaderLength = (ushort)(DbfHeader.Size + descriptors.Length * DbfFieldDescriptor.Size + 1),
            Language = DbfLanguage.ANSI,
            LastUpdate = DateOnly.FromDateTime(DateTime.Now),
            RecordCount = 0,
            RecordLength = (ushort)descriptors.Sum(static d => d.Length),
            TableFlags = GetDbfTableFlags(descriptors),
            Version = GetDbfVersion(descriptors),
        };
        Descriptors = descriptors;
        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();

        WriteHeader();

        static DbfVersion GetDbfVersion(ImmutableArray<DbfFieldDescriptor> descriptors)
        {
            var version = DbfVersion.DBase03;
            foreach (var descriptor in descriptors)
            {
                if (descriptor.Type == DbfFieldType.Memo)
                    version = DbfVersion.DBase83;
            }
            return version;
        }

        static DbfTableFlags GetDbfTableFlags(ImmutableArray<DbfFieldDescriptor> descriptors)
        {
            var flags = DbfTableFlags.None;
            foreach (var descriptor in descriptors)
            {
                if (descriptor.Type == DbfFieldType.Memo)
                    flags |= DbfTableFlags.HasMemoField;
            }
            return flags;
        }
    }

    public void Dispose()
    {
        WriteHeader();
        _stream.Dispose();
    }

    private void WriteHeader()
    {
        _stream.Position = 0;

        switch (_header.Version)
        {
            case DbfVersion.DBase02:
                DbfHeader02 header02 = _header;
                _stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header02, 1)));
                foreach (var descriptor in Descriptors)
                {
                    DbfFieldDescriptor02 descriptor02 = descriptor;
                    _stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref descriptor02, 1)));
                }
                _stream.WriteByte(0x0D);
                if (_stream.Position is not DbfHeader02.HeaderLength)
                {
                    _stream.Position = DbfHeader02.HeaderLength - 1;
                    _stream.WriteByte(0x00);
                }
                break;
            case DbfVersion.DBase03 or DbfVersion.DBase04 or DbfVersion.DBase05 or DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.DBase43 or DbfVersion.DBase63 or DbfVersion.DBase83 or DbfVersion.DBase8B or DbfVersion.DBaseCB or DbfVersion.FoxPro2WithMemo or DbfVersion.FoxBASE:
                _stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _header, 1)));
                _stream.Write(MemoryMarshal.AsBytes(Descriptors.AsSpan()));
                _stream.WriteByte(0x0D);
                break;
            default:
                throw new InvalidDataException($"Invalid DBF version '0x{(byte)_header.Version:X2}'");
        };
    }

    private void UpdateLastUpdate()
    {
        var header = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _header, 1));
        var now = DateTime.Now;
        ref var lastUpdateYear = ref MemoryMarshal.AsRef<byte>(
            header[..Marshal.OffsetOf<DbfHeader>(nameof(DbfHeader.LastUpdateYear)).ToInt32()]);
        lastUpdateYear = (byte)(now.Year - 1900);
        ref var lastUpdateMonth = ref MemoryMarshal.AsRef<byte>(
            header[..Marshal.OffsetOf<DbfHeader>(nameof(DbfHeader.LastUpdateMonth)).ToInt32()]);
        lastUpdateMonth = (byte)now.Month;
        ref var lastUpdateDay = ref MemoryMarshal.AsRef<byte>(
            header[..Marshal.OffsetOf<DbfHeader>(nameof(DbfHeader.LastUpdateDay)).ToInt32()]);
        lastUpdateDay = (byte)now.Day;
    }

    public void Write(params ImmutableArray<DbfRecord> records)
    {
        if (records.Length == 0)
        {
            return;
        }

        byte[]? pooledArray = null;
        try
        {
            var recordLength = _header.RecordLength;
            Span<byte> buffer = recordLength < 256
                ? stackalloc byte[recordLength]
                : (pooledArray = ArrayPool<byte>.Shared.Rent(recordLength)).AsSpan(0, recordLength);
            foreach (var record in records)
            {
                WriteRecord(record, Descriptors, Encoding, DecimalSeparator, buffer);
                _stream.Write(buffer);
            }
            RecordCount += (uint)records.Length;
        }
        finally
        {
            if (pooledArray is not null)
                ArrayPool<byte>.Shared.Return(pooledArray);
        }
    }

    internal static void WriteRecord(DbfRecord record, ImmutableArray<DbfFieldDescriptor> descriptors, Encoding encoding, char decimalSeparator, Span<byte> target)
    {
        target[0] = (byte)record.Status;
        var offset = 1;
        for (var i = 0; i < descriptors.Length; ++i)
        {
            WriteField(record[i], in descriptors.ItemRef(i), encoding, decimalSeparator, target.Slice(offset, descriptors[i].Length));
            offset += descriptors[i].Length;
        }
    }

    internal static void WriteField(DbfField field, in DbfFieldDescriptor descriptor, Encoding encoding, char decimalSeparator, Span<byte> target)
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
