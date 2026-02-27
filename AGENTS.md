# AGENTS.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Overview

A .NET library for reading/writing xBase/dBASE DBF (and memo DBT/FPT) files.
Priorities: **correctness** (format/spec) → **non-breaking APIs** → **performance** → **ergonomics**.

If anything is ambiguous, prefer the smallest safe change and leave a comment describing assumptions.

## Build & test commands

- Build: `dotnet build`
- Test all: `dotnet test`
- Run a single test: `dotnet test --filter "FullyQualifiedName~DBase.Tests.DBase03.DBase03.VerifyReader"`
- Run tests for one version fixture: `dotnet test --filter "FullyQualifiedName~DBase.Tests.DBase03"`
- Update Verify snapshots (when `.received.` files appear): review the diff, then rename `.received.` → `.verified.`

When changing parsing/serialization: always run tests (or add them if missing).

## Architecture

### Core types (src/DBase/)

- **`Dbf`** — Main entry point. Opens/creates DBF files via `Dbf.Open(path)` / `Dbf.Create(path, descriptors)`. Owns the DBF stream and optional `Memo`. Manages header read/write, record-level random access, and typed serialization through `EnumerateRecords<T>()` / `Add<T>(record)`.
- **`DbfHeader`** — 32-byte `readonly record struct` with `[StructLayout(LayoutKind.Explicit)]` mapping directly to the on-disk header. Contains version, record count, header/record lengths, language, table flags.
- **`DbfFieldDescriptor`** — 32-byte `readonly record struct` for field schema: name, type, length, decimal, flags. Has static factory methods (`Character(...)`, `Numeric(...)`, etc.) for constructing descriptors.
- **`DbfRecord`** / **`DbfField`** — The untyped record/field types. `DbfRecord` is `IReadOnlyList<DbfField>`. `DbfField` is a boxed-value wrapper with implicit/explicit conversions to/from CLR types.
- **`Memo`** — Reads/writes DBT (dBASE III/IV) and FPT (FoxPro) memo files. Uses delegate-based dispatch (`_get`/`_set`/`_len`) keyed by `DbfVersion` to handle format differences. Append-only writes.

### dBASE II support (Interop/)

`DbfHeader02` and `DbfFieldDescriptor02` are smaller layout structs for the older dBASE II format. They have implicit/explicit conversion operators to/from the standard `DbfHeader`/`DbfFieldDescriptor`, so the rest of the codebase works uniformly with the v3+ types.

### Serialization (Serialization/)

- **`DbfRecordSerializer<T>`** — Glues `TypeProjection<T>` and `DbfRecordFormatter<T>` to serialize/deserialize `T` against a byte span.
- **`TypeProjection<T>`** — Bidirectional Expression-tree compiled projection between `T` and `object?[]`. Special-cased for `DbfRecord`.
- **`DbfRecordFormatter<T>`** — Builds an `ImmutableArray<DbfFieldFormatter>` from descriptors and CLR property types, then reads/writes each field at its byte offset.
- **`DbfFieldFormatter`** (Serialization/Fields/) — Delegate pair (`ReadValue`/`WriteValue`). The static `Create` factory dispatches on `DbfFieldType` to per-type formatters (Character, Numeric, Date, Memo, etc.).

Serializers are cached per `(descriptors, typeof(T))` in `SerializerExtensions.GetSerializer<T>()`.

### Code generation (CodeGen/)

`RecordGenerator` generates C# `record` or `class` source text from a DBF file's field descriptors. Uses Humanizer for PascalCase conversion.

### Stream interop (Interop/StreamExtensions)

Extension methods `Read<T>()` / `Write<T>()` for reading/writing blittable structs from streams, with big-endian byte-swap support based on `[FieldOffset]` reflection.

## Test structure (tests/DBase.Tests/)

- Each supported version has a folder (e.g. `DBase03/`, `DBase30/`, `DBase83/`) containing:
  - A `.dbf` (and optionally `.dbt`/`.fpt`) fixture file
  - A test class inheriting `DBaseTest<T>` where `T` is a positional `record` matching that file's schema
  - Verify `.verified.` snapshot files for header, reader, writer, and memo outputs
- **`DBaseTest`** (base) provides `VerifyHeader`, `VerifyReader`, `VerifyWriter`, `VerifyMemo` using Verify + DiffPlex.
- **`DBaseTest<T>`** adds typed variants: `VerifyReaderTyped`, `VerifyWriterTyped`, `RoundtripHeader`, `RoundtripDescriptors`, `RoundtripRecords` that do byte-level equality checks on round-tripped data.
- Verify snapshots are stored beside the test class (configured in `ModuleInitializer.cs`). When tests produce `.received.` files, diff them against the `.verified.` files before accepting.

### Adding a test for a new dBASE version

1. Create a folder under `tests/DBase.Tests/` named after the version (e.g. `DBaseXX/`).
2. Place the `.dbf` (and memo) fixture file in that folder.
3. Create a test class inheriting `DBaseTest<TRecord>` with a record type matching the schema.
4. Run tests — Verify will generate `.received.` files. Review and rename to `.verified.`.

## Coding style

### Performance & allocations
- Prefer `ReadOnlySpan<byte>`/`Span<byte>` and slicing over allocating arrays.
- Avoid LINQ in hot paths; use `for` loops.
- Avoid hidden allocations (boxing, closures, enumerators) in tight loops.
- Prefer `readonly struct` / `record struct` for small immutable value objects.

### Types
- Prefer `byte`, `ushort`, etc. (no Win32 typedef names like `BYTE`, `WORD`).
- Use `TryXxx` patterns for expected failures (invalid header, truncated data, bad flags).
- Nullability is enabled: keep it correct; avoid `!` unless proven safe.

### Exceptions
- Throw for programmer error / invalid arguments.
- For invalid file content: prefer a well-named exception type or `Try...` returning `false` (follow existing API style).

## Binary file rules (DBF/DBT/FPT)

When touching parsing/writing code:

- Treat the on-disk format as authoritative. Be explicit about endianness, field offsets/sizes, encoding assumptions, flags/bitmasks, and reserved bytes.
- Never "fix up" unknown bytes unless the spec says so.
- Validate bounds before reading: no out-of-range slicing.
- Prefer deterministic output: stable ordering, padding, and default values for reserved bytes.

If you change a layout/offset:
- Add a test using a small hand-crafted byte buffer.
- Document the layout in XML docs or a nearby comment.

## XML documentation

Public (and usually protected) members require XML docs: `<summary>` always, plus `<param>`, `<returns>`, `<remarks>` as needed. Use `<see cref="..."/>`, `<c>...</c>`, `<paramref name="..."/>`. Prefer `/// <inheritdoc/>` when inherited docs are sufficient. Document non-obvious file-format behavior (trimming/padding, deleted-record markers, numeric parsing, memo block sizing, VFP quirks).

## Tests

- xUnit v3 with Verify for snapshot testing.
- Name tests `Method_Scenario_ExpectedResult`.
- For binary parsing/serialization: include edge cases (truncated buffers, invalid flags, max/min field sizes) and round-trip tests where possible.
- Avoid large golden files; prefer small byte arrays built in-test.

## PR hygiene

- Keep diffs small and reviewable.
- Don't mix refactors with behavior changes.
- If behavior changes, add/update tests and explain the rationale.

## When unsure

If you aren't sure about a format detail:
- Search for an existing implementation in this repo first.
- Then consult references linked in README.
- Capture the reasoning in a comment + test.
