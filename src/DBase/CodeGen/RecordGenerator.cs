using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Humanizer;

namespace DBase.CodeGen;

internal static partial class RecordGenerator
{
    private static readonly HashSet<string> s_cSharpKeywords =
    [
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    ];

    private static readonly Regex s_invalidCharsRegex = CreateInvalidCharsRegex();

    public static string GenerateRecord(string dbfPath, bool pascalCase = true)
    {
        var descriptors = ReadDescriptors(dbfPath);
        var typeName = GetTypeName(dbfPath, pascalCase);

        using var stream = new StringWriter();
        using var writer = new IndentedTextWriter(stream, "    ");
        writer.Write("public partial record ");
        writer.WriteLine(typeName);
        writer.WriteLine('(');
        writer.Indent++;

        var existingIdentifiers = new HashSet<string>();
        var offset = descriptors[^1].Offset;
        foreach (var descriptor in descriptors)
        {
            writer.Write(GetCSharpType(descriptor));
            writer.Write(' ');
            writer.Write(GetIdentifier(descriptor.Name.ToString(), pascalCase, existingIdentifiers));
            if (descriptor.Offset < offset)
                writer.WriteLine(",");
            else
                writer.WriteLine();
        }

        writer.Indent--;
        writer.WriteLine(");");

        return stream.ToString();
    }

    public static string GenerateClass(string dbfPath, bool pascalCase = true)
    {
        var descriptors = ReadDescriptors(dbfPath);
        var typeName = GetTypeName(dbfPath, pascalCase);

        using var stream = new StringWriter();
        using var writer = new IndentedTextWriter(stream, "    ");
        writer.Write("public partial class ");
        writer.WriteLine(typeName);
        writer.WriteLine('{');
        writer.Indent++;

        var existingIdentifiers = new HashSet<string>();
        foreach (var descriptor in descriptors)
        {
            writer.Write("public ");
            writer.Write(GetCSharpType(descriptor));
            writer.Write(' ');
            writer.Write(GetIdentifier(descriptor.Name.ToString(), pascalCase, existingIdentifiers));
            writer.WriteLine(" { get; set; }");
        }

        writer.Indent--;
        writer.WriteLine('}');

        return stream.ToString();
    }

    private static ImmutableArray<DbfFieldDescriptor> ReadDescriptors(string dbfPath)
    {
        using var stream = File.OpenRead(dbfPath);
        var header = Dbf.ReadHeader(stream);
        return Dbf.ReadDescriptors(stream, header.Version);
    }

    private static string GetTypeName(string dbfPath, bool pascalCase) =>
        $"{GetIdentifier(Path.GetFileNameWithoutExtension(dbfPath), pascalCase, existingIdentifiers: null)}Record";

    private static string GetCSharpType(DbfFieldDescriptor descriptor)
    {
        return descriptor.Type switch
        {
            DbfFieldType.AutoIncrement => "int",
            DbfFieldType.Binary => "byte[]",
            DbfFieldType.Blob => "byte[]",
            DbfFieldType.Character => "string",
            DbfFieldType.Currency => "decimal",
            DbfFieldType.Date => "DateOnly",
            DbfFieldType.DateTime => "DateTime",
            DbfFieldType.Double => "double",
            DbfFieldType.Float => "double",
            DbfFieldType.Int32 => "int",
            DbfFieldType.Logical => "bool?",
            DbfFieldType.Memo => "string",
            DbfFieldType.NullFlags => "byte[]",
            DbfFieldType.Numeric when descriptor.Decimal == 0 => "long",
            DbfFieldType.Numeric => "double",
            DbfFieldType.Ole => "byte[]",
            DbfFieldType.Picture => "byte[]",
            DbfFieldType.Timestamp => "DateTime",
            DbfFieldType.Variant => "string",
            _ => throw new NotSupportedException($"Unsupported field type: {descriptor.Type}"),
        };
    }

    [GeneratedRegex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]")]
    private static partial Regex CreateInvalidCharsRegex();

    private static string GetIdentifier(string identifier, bool pascalCase, HashSet<string>? existingIdentifiers)
    {
        var proposedIdentifier =
            identifier.Length > 1 && identifier[0] == '@'
                ? "@" + s_invalidCharsRegex.Replace(identifier[1..], "_")
                : s_invalidCharsRegex.Replace(identifier, "_");
        if (string.IsNullOrEmpty(proposedIdentifier))
        {
            proposedIdentifier = "_";
        }

        var firstChar = proposedIdentifier[0];
        if ((!char.IsLetter(firstChar) && firstChar != '_' && firstChar != '@') || s_cSharpKeywords.Contains(proposedIdentifier))
        {
            proposedIdentifier = "_" + proposedIdentifier;
        }

        if (pascalCase)
        {
            proposedIdentifier = proposedIdentifier.Underscore().Pascalize();
        }

        return MakeUnique(proposedIdentifier, existingIdentifiers);
    }

    private static string MakeUnique(string proposedIdentifier, HashSet<string>? existingIdentifiers)
    {
        if (existingIdentifiers is null)
        {
            return proposedIdentifier;
        }

        var finalIdentifier = proposedIdentifier;
        var suffix = 1;
        while (!existingIdentifiers.Add(finalIdentifier))
        {
            finalIdentifier = proposedIdentifier + suffix;
            suffix++;
        }

        return finalIdentifier;
    }
}
