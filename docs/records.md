---
title: Reading and writing
nav_order: 3
---

# Reading and writing records

## Inspect the schema

```csharp
using DBase;

using var table = Dbf.Open("people.dbf");
Console.WriteLine($"{table.Version}: {table.RecordCount} physical rows");
foreach (var field in table.Descriptors)
{
    Console.WriteLine($"{field.Name}: {field.Type}, {field.Length} bytes");
}
```

`Descriptors` is an immutable array defining field order, name, type, width, decimal places, and flags. `Version`, `Language`, `Encoding`, `LastUpdate`, and `DbcBacklink` expose table metadata.

## Untyped records

`EnumerateRecords()` yields `DbfRecord` values in file order. Each row implements `IReadOnlyList<DbfField>`, so fields are accessible by zero-based index. Use `GetValue<T>()` for a conversion, for example `row[0].GetValue<string>()`. Empty numeric, logical, and date values may require nullable CLR types.

`table[0]` and `table.GetRecord(0)` read a physical row directly. An index outside `0..RecordCount-1` throws `ArgumentOutOfRangeException`.

```csharp
using var table = Dbf.Open("people.dbf");
foreach (var row in table.EnumerateRecords())
{
    if (row.IsDeleted)
        continue;

    Console.WriteLine(row[0].GetValue<string>());
}
```

Enumeration and `RecordCount` include deleted rows. Untyped records expose their delete marker through `IsDeleted`; they are not filtered automatically.

## Typed records

`EnumerateRecords<T>()`, `GetRecord<T>(index)`, and `Add<T>(value)` map public instance properties in reflection order to fields in descriptor order. The number of mapped properties must equal the number of fields. A field attribute's `Name` controls descriptor generation when creating a table; it does not reorder an existing file's fields or bind properties by name.

A model needs either:

- A public constructor with parameters matching the mapped property types in order, such as a positional record.
- A public parameterless constructor and public setters on every mapped property.

Mark unrelated properties with `[DbfIgnore]`. Avoid relying on inherited property ordering; inspect the schema and use a simple model with explicit field order for external tables. Typed models do not expose the delete marker, so use untyped records when filtering deleted rows matters.

## Append and save

```csharp
using var table = Dbf.Open("people.dbf");
table.Add(new DbfRecord("Katherine", 101L));
table.Flush();
table.SaveAs("people-copy.dbf");
```

`Add` appends to the physical end of the table. Supply exactly one value per descriptor for untyped rows. New untyped records default to valid status; an explicit `new DbfRecord(DbfRecordStatus.Deleted, ...)` preserves a deleted marker. Typed appends use valid status.

`Flush()` writes pending header metadata and flushes both table and memo streams. Disposal also flushes. `SaveAs(path)` copies the table and its available memo file to new paths; existing destinations cause an `IOException`. `WriteTo(dbfStream, memoStream)` copies to caller-provided output streams, and only copies memo data when a memo destination is supplied.

These copy operations preserve the existing encoding bytes. They do not create or copy a `.cpg` sidecar, and they do not convert the table to another DBF version. If the source needs a sidecar to identify its encoding, copy that sidecar yourself.

`Dbf` uses a shared mutable stream position; serialize access to an instance and finish lazy enumeration before disposing it. There is currently no public row update, deletion, packing, or transaction API.
