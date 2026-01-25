using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Interop;

internal static class StreamExtensions
{
    extension(Stream stream)
    {
        public T Read<T>() where T : struct =>
            stream.TryRead(out T @struct) ? @struct : throw new EndOfStreamException();

        public bool TryRead<T>(out T @struct) where T : struct
        {
            Unsafe.SkipInit(out @struct);
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
                return false;

            if (BitConverter.IsLittleEndian)
                return true;

            // TODO: Does it make sense to cache this?
            var offsets = typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() is not null)
                .Select(f => Marshal.OffsetOf<T>(f.Name).ToInt32())
                .Append(Unsafe.SizeOf<T>())
                .ToArray();

            for (var i = 0; i < offsets.Length - 1; ++i)
                buffer[offsets[i]..offsets[i + 1]].Reverse();

            return true;
        }

        public void Write<T>(T @struct) where T : struct
        {
            var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
            if (!BitConverter.IsLittleEndian)
            {
                // TODO: Does it make sense to cache this?
                var offsets = typeof(T)
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<FieldOffsetAttribute>() is not null)
                    .Select(f => Marshal.OffsetOf<T>(f.Name).ToInt32())
                    .Append(Unsafe.SizeOf<T>())
                    .ToArray();

                for (var i = 0; i < offsets.Length - 1; ++i)
                    buffer[offsets[i]..offsets[i + 1]].Reverse();
            }

            stream.Write(buffer);
        }
    }
}
