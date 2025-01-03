using System.Runtime.InteropServices;

namespace DBase.Internal;
internal static class DbtHelper
{
    public static void WriteHeader(Stream stream, DbtHeader header)
    {
        stream.Position = 0;
        stream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
    }
}
