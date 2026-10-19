using System.Text;
using DBase;

var folder = Directory.CreateTempSubdirectory("DBase.Docs.").FullName;
try
{
    var peoplePath = Path.Combine(folder, "people.dbf");
    using (var table = Dbf.Create(
        peoplePath,
        [DbfFieldDescriptor.Character("NAME", 30), DbfFieldDescriptor.Numeric("AGE", 3)]))
    {
        table.Add("Ada", 36L);
        table.Add(new DbfRecord(DbfRecordStatus.Deleted, table.Descriptors, ["Grace", 85L]));
    }

    using (var table = Dbf.Open(peoplePath))
    {
        var first = table.GetRecord(0);
        Require(first[0].GetValue<string>() == "Ada" && first[1].GetValue<long?>() == 36, "Untyped fields");
        Require(table.RecordCount == 2 && table.GetRecord(1).IsDeleted, "Deleted-row status");
        table.SaveAs(Path.Combine(folder, "people-copy.dbf"));
    }

    var typedPath = Path.Combine(folder, "typed.dbf");
    using (var table = Dbf.Create<Person>(typedPath))
        table.Add(new Person("Ada", 36));
    using (var table = Dbf.Open(typedPath))
        Require(table.EnumerateRecords<Person>().Single() == new Person("Ada", 36), "Typed records");

    var notesPath = Path.Combine(folder, "notes.dbf");
    using (var table = Dbf.Create<Note>(notesPath))
        table.Add(new Note("Ada", "Longer text stored in a DBT file."));
    using (var table = Dbf.Open(notesPath))
    {
        Require(table.Memo is not null, "Memo companion");
        Require(table.GetRecord<Note>(0).Text == "Longer text stored in a DBT file.", "Memo text");
    }

    var attachmentsPath = Path.Combine(folder, "attachments.dbf");
    using (var table = Dbf.Create<Attachment>(attachmentsPath))
        table.Add(new Attachment("example.bin", [1, 2, 3]));
    using (var table = Dbf.Open(attachmentsPath))
        Require(table.GetRecord<Attachment>(0).Data.SequenceEqual(new byte[] { 1, 2, 3 }), "Binary memo");

    var utf8Path = Path.Combine(folder, "utf8.dbf");
    using (var table = Dbf.Create(
        utf8Path,
        [DbfFieldDescriptor.Character("NAME", 30)],
        new DbfCreateOptions { Encoding = Encoding.UTF8 }))
        table.Add("João");
    File.WriteAllText(Path.ChangeExtension(utf8Path, ".cpg"), "UTF-8");
    using (var table = Dbf.Open(utf8Path))
        Require(table[0][0].GetValue<string>() == "João", "Code-page sidecar");

    Console.WriteLine("Documentation examples completed successfully.");
}
finally
{
    Directory.Delete(folder, recursive: true);
}

static void Require(bool condition, string example)
{
    if (!condition)
        throw new InvalidOperationException($"Example failed: {example}.");
}

public sealed record Person(
    [property: DbfCharacter(30)] string Name,
    [property: DbfNumeric(3)] int Age);

public sealed record Note(
    [property: DbfCharacter(30)] string Name,
    [property: DbfMemo] string Text);

public sealed record Attachment(
    [property: DbfCharacter(30)] string Name,
    [property: DbfOle(4)] byte[] Data);
