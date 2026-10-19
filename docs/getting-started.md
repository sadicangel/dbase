---
title: Getting started
nav_order: 2
---

# Getting started

The current source targets .NET 11. Use a compatible SDK and runtime when building the repository or consuming a package built from it.

## Install

Add the NuGet package to your application:

```shell
dotnet add package DBase
```

For a prerelease package, add `--prerelease` or select its exact version with `--version`.

## Create and read a table

```csharp
using DBase;

// CreateNew semantics: choose a path that does not already exist.
using (var table = Dbf.Create(
    "people.dbf",
    [
        DbfFieldDescriptor.Character("NAME", 30),
        DbfFieldDescriptor.Numeric("AGE", 3)
    ]))
{
    table.Add("Ada", 36L);
    table.Add("Grace", 85L);
}

using var people = Dbf.Open("people.dbf");
foreach (var person in people.EnumerateRecords())
{
    var name = person[0].GetValue<string>();
    var age = person[1].GetValue<long?>();
    Console.WriteLine($"{name}: {age}");
}
```

Fields are stored in descriptor order. Character lengths are **bytes**, and numeric widths must accommodate the complete formatted value, including sign and decimal separator.

`Dbf` owns the table and memo handles. Keep it alive while enumerating records, and dispose it with `using` to flush header changes and close the files. `Dbf.Open(path)` currently opens files with read/write access, so the process needs write permission even when only reading.

## Use your own model

```csharp
using DBase;

using (var table = Dbf.Create<Person>("typed-people.dbf"))
{
    table.Add(new Person("Ada", 36));
}

using var people = Dbf.Open("typed-people.dbf");
foreach (var person in people.EnumerateRecords<Person>())
{
    Console.WriteLine(person.Name);
}

public sealed record Person(
    [property: DbfCharacter(30)] string Name,
    [property: DbfNumeric(3)] int Age);
```

Typed mapping follows public property order, rather than looking up columns by name. Keep the mapped properties in the same order as the file's fields; see [record mapping](records.md#typed-records).

The repository also includes a [runnable example](https://github.com/sadicangel/dbase/tree/main/docs/examples) that creates temporary tables and demonstrates typed records, memo fields, encoding overrides, and deleted-row status.
