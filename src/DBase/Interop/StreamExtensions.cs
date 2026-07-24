using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Interop;

internal static class StreamExtensions
{
    extension(Stream stream)
    {
        public T Read<T>() where T : unmanaged =>
            stream.TryRead(out T @struct) ? @struct : throw new EndOfStreamException();

        public bool TryRead<T>(out T @struct) where T : unmanaged
        {
            Unsafe.SkipInit(out @struct);
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
                return false;

            if (!BitConverter.IsLittleEndian)
                @struct.ReverseEndianness();

            return true;
        }

        public void Write<T>(T @struct) where T : unmanaged
        {
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            if (!BitConverter.IsLittleEndian)
                @struct.ReverseEndianness();

            stream.Write(buffer);
        }
    }
}
