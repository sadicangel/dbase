using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DBase;

/// <summary>
/// Describes a <see cref="DbfField" />.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
internal readonly struct DbfFieldDescriptor02
{
    internal const int Size = 16;

    [FieldOffset(0)]
    public readonly DbfFieldName Name;
    [FieldOffset(10)]
    private readonly byte _zero; // '\0'
    [FieldOffset(11)]
    public readonly DbfFieldType Type;
    [FieldOffset(12)]
    public readonly byte Length;
    [FieldOffset(13)]
    private readonly short _address; // in memory address.
    [FieldOffset(15)]
    public readonly byte Decimal;

    private unsafe string GetDebuggerDisplay() => $"{Name},{(char)Type},{Length},{Decimal}";

    public static implicit operator DbfFieldDescriptor(DbfFieldDescriptor02 descriptor) => new()
    {
        Name = descriptor.Name,
        Type = descriptor.Type,
        Length = descriptor.Length,
        Decimal = descriptor.Decimal
    };
}
