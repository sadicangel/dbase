using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

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
            LastUpdate = DateTime.Now,
            RecordCount = 0,
            RecordLength = (ushort)descriptors.Sum(static d => d.Length),
            TableFlags = GetDbfTableFlags(descriptors),
            Version = GetDbfVersion(descriptors),
        };
        Descriptors = descriptors;
        Encoding = _header.Language.GetEncoding();
        DecimalSeparator = _header.Language.GetDecimalSeparator();

        WriteHeader();
    }

    public void Dispose()
    {
        WriteHeader();
        _stream.Dispose();
    }

    private void WriteHeader()
    {
        _stream.Position = 0;
        _stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _header, 1)));
        _stream.Write(MemoryMarshal.AsBytes(Descriptors.AsSpan()));
        _stream.WriteByte(0x0D);
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


    internal static DbfVersion GetDbfVersion(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var version = DbfVersion.FoxBaseDBase3NoMemo;
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type == DbfFieldType.Memo)
                version = DbfVersion.FoxBaseDBase3WithMemo;
        }
        return version;
    }

    internal static DbfTableFlags GetDbfTableFlags(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var flags = DbfTableFlags.None;
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type == DbfFieldType.Memo)
                flags |= DbfTableFlags.HasMemoField;
        }
        return flags;
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
            if (descriptor.Type != DbfFieldType.Logical)
                target[0] = (byte)'?';
        }
        else
        {
            switch (descriptor.Type)
            {
                case DbfFieldType.Character:
                    var @string = ((string)field._value).AsSpan();
                    // Align left.
                    encoding.GetBytes(@string, target);
                    break;

                case DbfFieldType.Numeric when descriptor.Decimal == 0:
                    var integer = FormattableString.Invariant($"{field._value}").AsSpan();
                    // Truncate if needed.
                    integer = integer[..Math.Min(descriptor.Length, integer.Length)];
                    // Align left.
                    encoding.GetBytes(integer, target);
                    break;

                case DbfFieldType.Numeric:
                case DbfFieldType.Float:
                    var @double = FormattableString.Invariant($"{field._value}").AsSpan();
                    var idx = @double.IndexOf(decimalSeparator);
                    // No decimal separator.
                    if (idx < 0)
                    {
                        // Truncate if needed.
                        @double = @double[..Math.Min(descriptor.Length, @double.Length)];
                        // Align left.
                        encoding.GetBytes(@double, target);
                    }
                    else
                    {
                        // Whole part.
                        var whole = @double[..idx];
                        // Truncate if needed.
                        whole = whole[..Math.Min(descriptor.Length - descriptor.Decimal - 1, whole.Length)];
                        // Decimal part.
                        var fraction = @double[(idx + 1)..];
                        // Truncate if needed.
                        fraction = fraction[..Math.Min(descriptor.Decimal, fraction.Length)];
                        // Align left.
                        encoding.GetBytes(fraction, target);
                        encoding.GetBytes(whole, target[fraction.Length..]);
                        encoding.GetBytes(MemoryMarshal.CreateReadOnlySpan(ref decimalSeparator, 1), target.Slice(idx, 1));
                    }
                    break;

                case DbfFieldType.Int32:
                case DbfFieldType.AutoIncrement:
                    var i32 = (int)(long)field._value;
                    MemoryMarshal.Write(target, in i32);
                    break;

                case DbfFieldType.Double:
                    var f64 = (double)field._value;
                    MemoryMarshal.Write(target, in f64);
                    break;

                case DbfFieldType.Date:
                    var date = (DateTime)field._value;
                    for (int i = 0, y = date.Year; i < 4; ++i, y /= 10)
                        target[i] = (byte)(y % 10 + '0');
                    for (int i = 4, m = date.Month; i < 6; ++i, m /= 10)
                        target[i] = (byte)((m & 10) + '0');
                    for (int i = 6, d = date.Day; i < 8; ++i, d /= 10)
                        target[i] = (byte)((d & 10) + '0');
                    break;

                case DbfFieldType.Timestamp:
                    var timestamp = (DateTime)field._value;
                    var julian = (int)(timestamp.Date.ToOADate() + 2415018.5);
                    MemoryMarshal.Write(target[..4], in julian);
                    var milliseconds = (int)timestamp.TimeOfDay.TotalMilliseconds;
                    MemoryMarshal.Write(target[4..], in milliseconds);
                    break;

                case DbfFieldType.Logical:
                    var boolean = (bool)field._value;
                    target[0] = (byte)(boolean ? 'T' : 'F');
                    break;

                case DbfFieldType.Memo:
                case DbfFieldType.Binary:
                case DbfFieldType.Ole:
                    var binary = ((string)field._value).AsSpan();
                    encoding.GetBytes(binary, target);
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(descriptor.Type), (int)descriptor.Type, typeof(DbfFieldType));
            }
        }
    }

}
