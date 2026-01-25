using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DBase.Interop;

/// <summary>
/// Describes a <see cref="DbfField" />.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
internal readonly record struct DbfFieldDescriptor02
{
    internal const int Size = 16;

    [field: FieldOffset(0)] public DbfFieldName Name { get; init; }

    [FieldOffset(10)] private readonly byte _zero; // '\0'

    [field: FieldOffset(11)] public DbfFieldType Type { get; init; }

    [field: FieldOffset(12)] public byte Length { get; init; }

    [FieldOffset(13)] private readonly short _address; // in memory address.

    [field: FieldOffset(15)] public byte Decimal { get; init; }

    private string GetDebuggerDisplay() => $"{Name},{(char)Type},{Length},{Decimal}";

    public static implicit operator DbfFieldDescriptor(DbfFieldDescriptor02 descriptor) => new()
    {
        Name = descriptor.Name,
        Type = descriptor.Type,
        Length = descriptor.Length,
        Decimal = descriptor.Decimal
    };

    public static implicit operator DbfFieldDescriptor02(DbfFieldDescriptor descriptor) => new()
    {
        Name = descriptor.Name,
        Type = descriptor.Type,
        Length = descriptor.Length,
        Decimal = descriptor.Decimal
    };
}
