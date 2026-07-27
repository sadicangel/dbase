using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DBase;

internal static class DbfFieldDescriptorExtensions
{
    private static NotSupportedException CannotInfer(DbfFieldDescriptor descriptor) =>
        new($"Cannot infer a supported DBF version for field '{descriptor.Name}' with type '{(char)descriptor.Type}' ({descriptor.Type}). Specify a DBF version explicitly to create this descriptor set.");

    private static NotSupportedException CannotInferAutoIncrement(DbfFieldDescriptor descriptor) =>
        new($"Cannot infer a supported DBF version for auto-increment field '{descriptor.Name}'. Auto-increment inference requires an {DbfFieldType.Int32} field descriptor with {DbfFieldFlags.AutoIncrement} flags.");

    private static bool IsMemoBacked(DbfFieldDescriptor descriptor) =>
        descriptor.Type is DbfFieldType.Blob or DbfFieldType.Memo or DbfFieldType.Ole or DbfFieldType.Picture ||
        descriptor.Type is DbfFieldType.Binary && descriptor.Length is not 8;

    private static bool HasAutoIncrementFlag(DbfFieldDescriptor descriptor) =>
        (descriptor.Flags & DbfFieldFlags.AutoIncrement) == DbfFieldFlags.AutoIncrement;

    private static void ApplyFlags(
        DbfFieldDescriptor descriptor,
        ref bool requiresFoxPro,
        ref bool requiresAutoIncrement)
    {
        if (descriptor.Flags is DbfFieldFlags.None)
        {
            return;
        }

        if (HasAutoIncrementFlag(descriptor))
        {
            if (descriptor.Type is not DbfFieldType.Int32)
            {
                throw CannotInferAutoIncrement(descriptor);
            }

            requiresAutoIncrement = true;
            return;
        }

        requiresFoxPro = true;
    }

    private static DbfVersion InferVersion(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        var hasMemo = false;
        var requiresDBase4 = false;
        var requiresFoxPro = false;
        var requiresAutoIncrement = false;
        var requiresFoxProWithVarchar = false;

        foreach (var descriptor in descriptors)
        {
            switch (descriptor.Type)
            {
                case DbfFieldType.Character:
                case DbfFieldType.Numeric:
                case DbfFieldType.Date:
                case DbfFieldType.Logical:
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Memo:
                    hasMemo = true;
                    if (descriptor.Length is 4)
                    {
                        requiresFoxPro = true;
                    }

                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Float:
                    requiresDBase4 = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Currency:
                case DbfFieldType.DateTime:
                case DbfFieldType.Int32:
                case DbfFieldType.NullFlags:
                    requiresFoxPro = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Binary when descriptor.Length is 8:
                    requiresFoxPro = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Ole:
                case DbfFieldType.Picture:
                    hasMemo = true;
                    requiresFoxPro = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Blob:
                    hasMemo = true;
                    requiresFoxProWithVarchar = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.Variant:
                    requiresFoxProWithVarchar = true;
                    ApplyFlags(descriptor, ref requiresFoxPro, ref requiresAutoIncrement);
                    break;

                case DbfFieldType.AutoIncrement:
                case DbfFieldType.Binary:
                case DbfFieldType.Double:
                case DbfFieldType.Timestamp:
                default:
                    throw CannotInfer(descriptor);
            }
        }

        if (requiresFoxProWithVarchar)
        {
            return DbfVersion.VisualFoxProWithVarchar;
        }

        if (requiresAutoIncrement)
        {
            return DbfVersion.VisualFoxProWithAutoIncrement;
        }

        if (requiresFoxPro)
        {
            return DbfVersion.VisualFoxPro;
        }

        if (requiresDBase4)
        {
            return hasMemo ? DbfVersion.DBase8B : DbfVersion.DBase04;
        }

        return hasMemo ? DbfVersion.DBase83 : DbfVersion.DBase03;
    }

    extension(ImmutableArray<DbfFieldDescriptor> descriptors)
    {
        public DbfVersion ResolveVersion(DbfVersion version) =>
            version is DbfVersion.Unspecified ? InferVersion(descriptors) : version;

        public bool HasMemoFields()
        {
            foreach (var descriptor in descriptors)
            {
                if (IsMemoBacked(descriptor))
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
