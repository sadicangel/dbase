namespace DBase.Interop;

internal readonly record struct BinaryDecimal(int Low, int Mid, int High, byte ScaleAndSign)
{
    public bool IsNegative => (ScaleAndSign & 0x80) != 0;

    public byte Scale => (byte)(ScaleAndSign & 0x7F);

    public BinaryDecimal(int low, int mid, int high, bool isNegative, byte scale)
        : this(low, mid, high, (byte)((scale & 0x7F) | (isNegative ? 0x80 : 0))) { }

    public static BinaryDecimal FromDecimal(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        return new BinaryDecimal(bits[0], bits[1], bits[2], isNegative: bits[3] < 0, scale: (byte)((bits[3] >> 16) & 0x7F));
    }

    public decimal ToDecimal() => new(Low, Mid, High, IsNegative, Scale);
}
