using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DBase.Interop;

internal readonly record struct FieldByteRange(int Offset, int Length);

internal static class EndianSensitiveFields<T> where T : unmanaged
{
    // ReSharper disable once StaticMemberInGenericType
    // Intentionally using a static member in a generic type to cache the ranges for each specific type T.
    private static readonly Lazy<ImmutableArray<FieldByteRange>> s_values = new(Create);

    public static ImmutableArray<FieldByteRange> Ranges => s_values.Value;

    private static ImmutableArray<FieldByteRange> Create()
    {
        var ranges = EnumerateRanges(typeof(T), baseOffset: 0)
            .OrderBy(x => x.Offset)
            .ToImmutableArray();
        ValidateRanges(ranges);
        return ranges;
    }

    private static IEnumerable<FieldByteRange> EnumerateRanges(Type type, int baseOffset)
    {
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in fields)
        {
            var offset = baseOffset + Marshal.OffsetOf(type, field.Name).ToInt32();
            if (TryGetEndianSensitiveLength(field.FieldType, out var length))
            {
                yield return new FieldByteRange(offset, length);
                continue;
            }

            if (TryGetInlineArrayInfo(field.FieldType, out var elementType, out var elementCount, out var elementOffset))
            {
                foreach (var range in EnumerateInlineArrayRanges(elementType, elementCount, offset + elementOffset))
                    yield return range;

                continue;
            }

            if (!ShouldRecurse(field.FieldType))
            {
                continue;
            }

            foreach (var range in EnumerateRanges(field.FieldType, offset))
                yield return range;
        }
    }

    private static void ValidateRanges(ImmutableArray<FieldByteRange> ranges)
    {
        for (var i = 1; i < ranges.Length; ++i)
        {
            var previous = ranges[i - 1];
            var current = ranges[i];
            if (previous.Offset + previous.Length > current.Offset)
                throw new InvalidOperationException($"Type {typeof(T).FullName} has overlapping endian-sensitive scalar fields.");
        }
    }

    private static IEnumerable<FieldByteRange> EnumerateInlineArrayRanges(Type elementType, int elementCount, int baseOffset)
    {
        var elementSize = GetStorageLength(elementType);
        for (var i = 0; i < elementCount; ++i)
        {
            var offset = baseOffset + i * elementSize;
            if (TryGetEndianSensitiveLength(elementType, out var length))
            {
                yield return new FieldByteRange(offset, length);
                continue;
            }

            if (!ShouldRecurse(elementType))
            {
                continue;
            }

            foreach (var range in EnumerateRanges(elementType, offset))
                yield return range;
        }
    }

    private static bool TryGetEndianSensitiveLength(Type type, out int length)
    {
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        length = Type.GetTypeCode(type) switch
        {
            TypeCode.Int16 or TypeCode.UInt16 => 2,
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
            TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double => 8,
            _ => 0
        };

        return length > 1;
    }

    private static bool TryGetInlineArrayInfo(Type type, out Type elementType, out int length, out int elementOffset)
    {
        var attribute = type.GetCustomAttribute<InlineArrayAttribute>();
        if (attribute is null)
        {
            elementType = typeof(void);
            length = 0;
            elementOffset = 0;
            return false;
        }

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (fields.Length != 1)
            throw new InvalidOperationException($"Inline array type {type.FullName} must have exactly one instance field.");

        var field = fields[0];
        elementType = field.FieldType;
        length = attribute.Length;
        elementOffset = Marshal.OffsetOf(type, field.Name).ToInt32();
        return true;
    }

    private static int GetStorageLength(Type type)
    {
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
        }

        var length = Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean or TypeCode.Byte or TypeCode.SByte => 1,
            TypeCode.Char or TypeCode.Int16 or TypeCode.UInt16 => 2,
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Single => 4,
            TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Double => 8,
            _ => 0
        };

        return length > 0 ? length : Marshal.SizeOf(type);
    }

    private static bool ShouldRecurse(Type type) =>
        type is { IsValueType: true, IsPrimitive: false, IsEnum: false }
        && type.GetCustomAttribute<InlineArrayAttribute>() is null;
}
