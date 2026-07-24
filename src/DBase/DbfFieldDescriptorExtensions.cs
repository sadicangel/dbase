using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DBase;

internal static class DbfFieldDescriptorExtensions
{
    extension(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        public bool HasMemoFields()
        {
            foreach (var descriptor in descriptors)
            {
                if (descriptor.Type is DbfFieldType.Blob or DbfFieldType.Memo or DbfFieldType.Ole or DbfFieldType.Picture ||
                    descriptor.Type is DbfFieldType.Binary && descriptor.Length is not 8)
                {
                    return true;
                }
            }

            return false;
        }

        public DbfTableFlags GetTableFlags()
        {
            if (descriptors.HasMemoFields())
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
