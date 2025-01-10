using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Internal;
internal static class StreamExtensions
{
    public static T Read<T>(this Stream stream) where T : struct =>
        stream.TryRead(out T @struct) ? @struct : throw new EndOfStreamException();

    public static bool TryRead<T>(this Stream stream, out T @struct) where T : struct
    {
        Unsafe.SkipInit(out @struct);
        var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
        if (stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) != buffer.Length)
        {
            return false;
        }

        if (BitConverter.IsLittleEndian)
        {
            return true;
        }

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

    public static void Write<T>(this Stream stream, T @struct) where T : struct
    {
        var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref @struct, 1));
        if (!BitConverter.IsLittleEndian)
        {
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
