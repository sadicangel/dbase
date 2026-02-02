using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DBase;

internal static class DbfFieldDescriptorExtensions
{
    extension(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        public DbfTableFlags GetTableFlags()
        {
            if (descriptors.Any(descriptor => descriptor.Type is DbfFieldType.Binary or DbfFieldType.Blob or DbfFieldType.Memo))
            {
                return DbfTableFlags.HasMemoField;
            }

            return DbfTableFlags.None;
        }

        public void EnsureFieldOffsets()
        {
            var offset = 1; // Skip the record deleted flag.
            for (var i = 0; i < descriptors.Length; ++i)
            {
                Unsafe.AsRef(in descriptors.ItemRef(i).Offset) = offset;
                offset += descriptors[i].Length;
            }
        }
    }
}
