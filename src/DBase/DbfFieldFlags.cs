namespace DBase;

[Flags]
public enum DbfFieldFlags : byte
{
    Node = 0x0,
    System = 0x1,
    Nullable = 0x2,
    Binary = 0x4,
    AutoIncrement = 0xC,
}
