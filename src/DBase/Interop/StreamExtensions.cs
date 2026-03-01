using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Interop;

internal static class StreamExtensions
{
    private static class FieldOffsets<T> where T : unmanaged
    {
        public static readonly int[] Values = typeof(T)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(f => Marshal.OffsetOf<T>(f.Name).ToInt32())
            .Append(Unsafe.SizeOf<T>())
            .ToArray();
    }

    internal static void ReverseFieldBytes<T>(Span<byte> buffer) where T : unmanaged
    {
        var offsets = FieldOffsets<T>.Values;
        for (var i = 0; i < offsets.Length - 1; ++i)
            buffer[offsets[i]..offsets[i + 1]].Reverse();
    }

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
                ReverseFieldBytes<T>(buffer);

            return true;
        }

        public void Write<T>(T @struct) where T : unmanaged
        {
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            if (!BitConverter.IsLittleEndian)
                ReverseFieldBytes<T>(buffer);

            stream.Write(buffer);
        }
    }
}
