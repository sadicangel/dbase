using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Interop;

[StructLayout(LayoutKind.Explicit, Size = Size)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
internal readonly record struct DbfFieldDescriptor02
{
    internal const int Size = 16;

    [field: FieldOffset(0)] public DbfFieldName Name { get; init; }

    [FieldOffset(10)] private readonly byte _zero; // '\0'

    [field: FieldOffset(11)] public DbfFieldType Type { get; init; }

    [field: FieldOffset(12)] public byte Length { get; init; }

    [FieldOffset(13)] internal readonly short Offset;

    [field: FieldOffset(15)] public byte Decimal { get; init; }

    private string GetDebuggerDisplay() => $"{Name},{(char)Type},{Length},{Decimal}";

    public static implicit operator DbfFieldDescriptor(DbfFieldDescriptor02 descriptor)
    {
        var v3 = new DbfFieldDescriptor
        {
            Name = descriptor.Name,
            Type = descriptor.Type,
            Length = descriptor.Length,
            Decimal = descriptor.Decimal
        };
        Unsafe.AsRef(in v3.Offset) = descriptor.Offset;

        return v3;
    }

    public static implicit operator DbfFieldDescriptor02(DbfFieldDescriptor descriptor)
    {
        var v2 = new DbfFieldDescriptor02
        {
            Name = descriptor.Name,
            Type = descriptor.Type,
            Length = descriptor.Length,
            Decimal = descriptor.Decimal
        };
        Unsafe.AsRef(in v2.Offset) = (short)descriptor.Offset;

        return v2;
    }
}
