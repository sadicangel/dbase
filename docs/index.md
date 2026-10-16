---
title: Home
nav_order: 1
---

# DBase for .NET

Read and write xBase `.dbf` tables, with sibling `.dbt` and `.fpt` memo files. Use `DbfRecord` when the schema is discovered at runtime, or map a table to your own .NET record or class.

```csharp
using DBase;

using var dbf = Dbf.Open("customers.dbf");
foreach (var row in dbf.EnumerateRecords())
{
    Console.WriteLine(row[0]);
}
```

- [Get started](getting-started.md): install the package and create your first table.
- [Read and write records](records.md): inspect schemas, use typed models, and preserve deleted records.
- [Configure fields](fields.md): select descriptors, attributes, and a DBF version.
- [Text encoding](encoding.md): resolve language markers and `.cpg` files, and handle byte limits.
- [Memo files](memo.md): work with DBT/FPT storage and binary values.
- [Compatibility and errors](compatibility.md): understand supported formats and current limits.
- [API reference]({{ '/api/' | relative_url }}): browse public types, members, parameters, and return values.

These pages describe the source on the repository's `main` branch. A published package may have fewer APIs; use the source at its release tag when checking version-specific behavior. DBase is licensed under the [MIT License](https://github.com/sadicangel/dbase/blob/main/LICENSE).
