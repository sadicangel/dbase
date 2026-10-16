---
title: Compatibility and errors
nav_order: 7
---

# Compatibility and errors

## Format coverage

The repository has fixture-based header, reader, writer, and byte-roundtrip tests for these version markers:

| Marker | Fixture family |
| --- | --- |
| `0x02` | dBASE II |
| `0x03` | dBASE III |
| `0x30`, `0x31`, `0x32` | Visual FoxPro variants |
| `0x83` | dBASE III with DBT memo |
| `0x8B` | dBASE IV with DBT memo |
| `0xF5` | FoxPro 2.x with FPT memo |

`DbfVersion` names additional historical markers. An enum value or field attribute does not imply complete coverage of every extension in that format. For example, SQL memo marker `DBaseCB` does not have a supported memo implementation. Use small representative files from the producing application to verify interoperability.

Current limits include:

- Path-based `Open` needs write access; public stream-based opening and asynchronous APIs are not exposed.
- Typed models map by property order, with one mapped property per field; typed rows do not expose deleted status.
- There are no public in-place row updates, delete/undelete, pack, index maintenance, query engine, or transaction APIs.
- Null-flag bytes are accessible as a field, but Visual FoxPro null bitmap semantics are not automatically projected into nullable CLR values.
- Auto-increment fields store supplied integers; `Add` does not generate IDs.
- Date-only fields discard time; binary date/time fields do not preserve an original time-zone offset.
- Copy operations preserve existing DBF/memo bytes and do not copy `.cpg` sidecars or convert formats.
- The C# record/class generator in the source is currently internal and is not a public package API.

## Diagnose field failures

Field conversion and field I/O errors at a record boundary are wrapped in `DbfSerializationException`:

```csharp
using DBase;

try
{
    using var table = Dbf.Open("people.dbf");
    foreach (var row in table.EnumerateRecords())
        Console.WriteLine(row);
}
catch (DbfSerializationException error)
{
    Console.Error.WriteLine(
        $"{error.Operation}: row {error.RecordIndex}, " +
        $"field {error.FieldIndex} ({error.FieldName})");
    Console.Error.WriteLine(error.InnerException?.Message);
}
```

The exception exposes zero-based **physical** record and field indices, the descriptor, target CLR field and record types, table version, and language marker. The original formatter exception remains in `InnerException`. Catch the wrapper at a read/write boundary when handling `FormatException`, `OverflowException`, or other field errors.

Schema validation and model-construction errors remain separate. Cancellation and unexpected runtime failures propagate unchanged. Malformed table geometry, inconsistent record lengths, truncated headers, or truncated record regions are rejected when opening with `InvalidDataException` or `EndOfStreamException`; file-system failures also remain separate.

## Contributing

Build with `dotnet build` and run `dotnet test` from the repository root. Parsing or serialization changes need edge-case tests and roundtrip coverage. Fixture tests use xUnit v3 and Verify; review `.received.` snapshots before accepting them as `.verified.` files.

The [source repository](https://github.com/sadicangel/dbase) contains the format references and contribution instructions. See [the documentation contributor guide](https://github.com/sadicangel/dbase/blob/main/docs/README.md) for local site builds and GitHub Pages setup.
