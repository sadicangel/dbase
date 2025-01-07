using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DBase;

internal static class DbfFieldDescriptorExtensions
{
    public static DbfTableFlags GetTableFlags(this ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var flags = DbfTableFlags.None;
        foreach (var descriptor in descriptors)
        {
            if (descriptor.Type == DbfFieldType.Memo)
                flags |= DbfTableFlags.HasMemoField;
        }
        return flags;
    }

    public static void EnsureFieldOffsets(this ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var offset = 1; // Skip the record deleted flag.
        for (var i = 0; i < descriptors.Length; ++i)
        {
            Unsafe.AsRef(in descriptors.ItemRef(i).Offset) = offset;
            offset += descriptors[i].Length;
        }
    }
}
