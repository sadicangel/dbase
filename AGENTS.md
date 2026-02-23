# AGENTS.md — dbase (C# / .NET) agent instructions

This repo implements a .NET library for reading/writing xBase/dBASE DBF (and memo DBT/FPT) files.
Priorities: **correctness** (format/spec) → **non-breaking APIs** → **performance** → **ergonomics**.

If anything is ambiguous, prefer the smallest safe change and leave a comment describing assumptions.

---

## Repository quick start (agent)

Typical commands (run from repo root):
- Build: `dotnet build`
- Test: `dotnet test`

When changing parsing/serialization: always run tests (or add them if missing).

---

## 1) Coding style (project preferences)

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
- For invalid file content: prefer a well-named exception type or `Try...` returning `false` (choose what the existing API style in the repo already does).

---

## 2) Binary file rules (DBF/DBT/FPT)

When touching parsing/writing code:

- Treat the on-disk format as authoritative. Be explicit about:
  - endianness
  - field offsets and sizes
  - encoding assumptions (ASCII/OEM/CodePage)
  - flags/bitmasks and reserved bytes

- Never “fix up” unknown bytes unless the spec says so.
- Validate bounds before reading: no out-of-range slicing.
- Prefer deterministic output:
  - stable ordering
  - stable padding
  - stable default values for reserved bytes

If you change a layout/offset:
- add a test using a small hand-crafted byte buffer
- and document the layout in XML docs or a nearby comment.

---

## 3) XML documentation (dotnet-ish)

This library is meant to be consumed via IDE tooling, so XML docs matter.

### Public surface
For `public` (and usually `protected`) members:
- Provide XML docs: `<summary>` always, plus `<param>`, `<returns>`, `<remarks>` as needed.
- Use `<see cref="..."/>` for types/members and `<c>...</c>` for literals.
- Use `<paramref name="..."/>` when referencing parameters.
- Prefer one-sentence summaries:
  - “Represents …”
  - “Reads …”
  - “Writes …”
  - “Gets …”

### Inheritdoc
- Prefer `/// <inheritdoc/>` when implementing an interface/override and inherited docs are sufficient.
- Add `<remarks>` only for extra constraints/behavior.

### What to document in this repo
- Any non-obvious file-format behavior:
  - trimming/padding rules
  - “deleted record” markers
  - numeric parsing (decimal separator, ‘.’ handling)
  - memo block sizing/headers
  - Visual FoxPro-specific quirks

---

## 4) Tests

- Prefer xUnit.
- Name tests `Method_Scenario_ExpectedResult`.
- For binary parsing/serialization:
  - include edge cases: truncated buffers, invalid flags, max/min field sizes
  - include a round-trip test where possible.

Avoid large golden files unless necessary; prefer small byte arrays built in-test.

---

## 5) PR hygiene

- Keep diffs small and reviewable.
- Don’t mix refactors with behavior changes.
- If behavior changes, add/update tests and explain the rationale in the PR/summary.

---

## 6) When unsure

If you aren’t sure about a format detail:
- search for an existing implementation in this repo first
- then consult references linked in README
- and capture the reasoning in a comment + test.