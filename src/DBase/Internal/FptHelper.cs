using System.Runtime.InteropServices;

namespace DBase.Internal;
internal static class FptHelper
{
    public static void WriteHeader(Stream stream, FptHeader header)
    {
        stream.Position = 0;
        stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
    }
}
