using System.Runtime.InteropServices;

namespace DBase.Interop;

internal static class EndiannessExtensions
{
    extension<T>(ref T @struct) where T : unmanaged
    {
        public void ReverseEndianness()
        {
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            foreach (var range in EndianSensitiveFields<T>.Ranges)
                buffer.Slice(range.Offset, range.Length).Reverse();
        }
    }
}
