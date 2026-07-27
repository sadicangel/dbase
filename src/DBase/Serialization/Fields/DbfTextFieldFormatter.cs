using System.Text;

namespace DBase.Serialization.Fields;

internal static class DbfTextFieldFormatter
{
    public static int WriteTruncated(Span<byte> target, ReadOnlySpan<char> value, Encoding encoding)
    {
        if (target.IsEmpty || value.IsEmpty)
        {
            return 0;
        }

        var encoder = encoding.GetEncoder();
        encoder.Convert(value, target, flush: true, out _, out var bytesWritten, out _);
        return bytesWritten;
    }
}
