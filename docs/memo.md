---
title: Memo files
nav_order: 6
---

# Memo files

A DBF memo field stores a pointer into a separate block-addressed file. Keep that sibling file with the DBF when moving or copying data.

| DBF version | Memo extension and format |
| --- | --- |
| `DBase83` (`0x83`) | `.dbt`, dBASE III |
| `DBase8B` (`0x8B`) | `.dbt`, dBASE IV |
| `FoxPro2WithMemo` (`0xF5`) | `.fpt`, FoxPro |
| `VisualFoxPro` (`0x30`) | `.fpt`, FoxPro |
| `VisualFoxProWithAutoIncrement` (`0x31`) | `.fpt`, FoxPro |
| `VisualFoxProWithVarchar` (`0x32`) | `.fpt`, FoxPro |

`Dbf.Open` looks for the extension appropriate to the version, and opens it when present. A missing sibling leaves `Dbf.Memo` as `null`; memo field reads currently return empty values and writes cannot persist memo content. Check `Memo` explicitly when a complete memo-bearing dataset is required.

## Create text memo fields

```csharp
using DBase;

using var table = Dbf.Create(
    "notes.dbf",
    [DbfFieldDescriptor.Character("NAME", 30), DbfFieldDescriptor.Memo("NOTES")]);
table.Add("Ada", "Longer text stored outside the DBF record.");
```

This schema infers `DBase83` and automatically creates `notes.dbt`. Selecting a version without supported memo storage throws `NotSupportedException` before creating the files. Existing DBF or memo destinations are not overwritten.

Typed models can use `[DbfMemo]` on text properties. Text memo fields use the table's resolved encoding. Object, blob, and picture memo values exposed as strings, including untyped `DbfField` values, use Base64; compatible typed `byte[]` properties access the raw bytes.

```csharp
public sealed record Attachment(
    [property: DbfCharacter(30)] string Name,
    [property: DbfOle(4)] byte[] Data);

// In application code:
// using var table = Dbf.Create<Attachment>("attachments.dbf");
// table.Add(new Attachment("example.bin", new byte[] { 1, 2, 3 }));
```

Memo writes append blocks. They do not reclaim obsolete entries or compact storage, and a failed table append is not rolled back transactionally across both files.

## Inspect memo storage

`Dbf.Memo` is enumerable in physical memo order. `Memo[index]` uses a **block index**, not a zero-based ordinal in that enumeration. Independent `Memo.Open(path, version)` and `Memo.Create(path, version, blockLength)` APIs are available when working directly with memo files; choose the matching DBF version because memo framing and endianness differ.

`Dbf.SaveAs` copies the available memo sibling automatically. `Dbf.WriteTo` needs a separate memo output stream, or only the DBF is copied.
