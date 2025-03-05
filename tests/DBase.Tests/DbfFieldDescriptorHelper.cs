using System.Collections.Frozen;

namespace DBase.Tests;
internal static class DbfFieldDescriptorHelper
{
    private static readonly FrozenDictionary<Type, string> s_aliases = new Dictionary<Type, string>()
    {
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(object)] = "object",
        [typeof(bool)] = "bool",
        [typeof(char)] = "char",
        [typeof(string)] = "string",
        [typeof(void)] = "void",
        [typeof(nint)] = "nint",
        [typeof(nuint)] = "nuint",

        [typeof(byte?)] = "byte?",
        [typeof(sbyte?)] = "sbyte?",
        [typeof(short?)] = "short?",
        [typeof(ushort?)] = "ushort?",
        [typeof(int?)] = "int?",
        [typeof(uint?)] = "uint?",
        [typeof(long?)] = "long?",
        [typeof(ulong?)] = "ulong?",
        [typeof(float?)] = "float?",
        [typeof(double?)] = "double?",
        [typeof(decimal?)] = "decimal?",
        [typeof(bool?)] = "bool?",
        [typeof(char?)] = "char?",
        [typeof(nint?)] = "nint?",
        [typeof(nuint?)] = "nuint?",

        [typeof(DateTime?)] = "DateTime?",
    }.ToFrozenDictionary();

    public static string GetRecordDefinition(this IEnumerable<DbfFieldDescriptor> descriptors, string name)
    {
        return $$"""
            public record {{name}}(
            {{string.Join($",{Environment.NewLine}", descriptors.Select(GetPropertyDefinition))}});
            """;

        static string GetPropertyDefinition(DbfFieldDescriptor descriptor)
        {
            var type = descriptor.GetDefaultDotNetType();
            var name = descriptor.Name.ToString();
            return $"\t{s_aliases.GetValueOrDefault(type, type.ToString())} {name}";
        }
    }

    public static Type GetDefaultDotNetType(this DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => typeof(long),
            DbfFieldType.Binary when descriptor.Length is 8 => typeof(double),
            DbfFieldType.Binary => typeof(string),
            DbfFieldType.Blob => typeof(string),
            DbfFieldType.Character => typeof(string),
            DbfFieldType.Currency => typeof(decimal),
            DbfFieldType.Date => typeof(DateTime?),
            DbfFieldType.DateTime => typeof(DateTime?),
            DbfFieldType.Double => typeof(double),
            DbfFieldType.Float => typeof(double?),
            DbfFieldType.Int32 => typeof(int),
            DbfFieldType.Logical => typeof(bool?),
            DbfFieldType.Memo => typeof(string),
            DbfFieldType.NullFlags => typeof(string),
            DbfFieldType.Numeric when descriptor.Length is 0 => typeof(long?),
            DbfFieldType.Numeric => typeof(double?),
            DbfFieldType.Ole => typeof(string),
            DbfFieldType.Picture => typeof(string),
            DbfFieldType.Timestamp => typeof(DateTime?),
            DbfFieldType.Variant => typeof(string),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor)),
        };
    }
}
