---
title: Fields and attributes
nav_order: 4
---

# Fields and attributes

## Explicit descriptors

Pass an immutable array of `DbfFieldDescriptor` values to `Dbf.Create`. Factory methods describe the on-disk representation:

```csharp
using DBase;

using var table = Dbf.Create(
    "orders.dbf",
    [
        DbfFieldDescriptor.Character("CUSTOMER", 40),
        DbfFieldDescriptor.Numeric("TOTAL", 12, 2),
        DbfFieldDescriptor.Date("PLACED"),
        DbfFieldDescriptor.Logical("PAID")
    ]);
```

Names use a fixed 10-byte ASCII payload. Longer names are truncated; choose unique names within that limit. Empty, whitespace-containing, and non-ASCII names are invalid.

## Typed schema generation

`Dbf.Create<T>(path, options)` generates descriptors from public instance properties. Add one field attribute to a property to select an exact representation:

```csharp
public sealed record Order(
    [property: DbfCharacter(40, Name = "CUSTOMER")] string Customer,
    [property: DbfNumeric(12, 2)] decimal Total,
    [property: DbfDate] DateOnly Placed,
    [property: DbfLogical] bool? Paid);
```

All field attributes accept `Name` and `Flags`. `[DbfIgnore]` excludes a property from both descriptor generation and serialization. Combining it with a field attribute, or applying multiple field attributes to one property, is rejected.

| Attribute | Representation |
| --- | --- |
| `DbfCharacter(length)` | Fixed-width text |
| `DbfNumeric(length, decimal)` / `DbfFloat(length, decimal)` | Numeric text with explicit width and decimal places |
| `DbfDate` / `DbfDateTime` / `DbfTimestamp` | Date or binary date/time |
| `DbfLogical` | Logical value, including unknown values with nullable models |
| `DbfInt32` / `DbfAutoIncrement` | Binary integer representations |
| `DbfCurrency` / `DbfDouble` | Binary currency/double |
| `DbfMemo(length)` | Text in memo storage |
| `DbfBinary(length)` / `DbfBlob` / `DbfOle(length)` / `DbfPicture(length)` | Binary or memo-backed representations |
| `DbfVariant(length)` | Variable-length text representation |
| `DbfNullFlags(length)` | Raw null-flag storage |

Not every representation is supported by every format or CLR property type. For example, `DbfBinary(8)` stores a binary double; other binary widths refer to memo storage. See [memo behavior](memo.md).

Without an attribute, common types use these defaults:

| CLR property | Inferred descriptor |
| --- | --- |
| `string`, `char[]`, `ReadOnlyMemory<char>` | Character, 100 bytes |
| `int`, `uint` | Numeric, 10 bytes, no decimal places |
| `long`, `ulong` | Numeric, 20 bytes, no decimal places |
| `float`, `double` | Numeric, 20 bytes, 8 decimal places |
| `bool` | Logical |
| `DateOnly` | Date |
| `DateTime` | Date; DateTime for an explicitly selected Visual FoxPro version |
| `decimal` | Currency only for an explicitly selected Visual FoxPro version |
| `DateTimeOffset` | DateTime only for an explicitly selected Visual FoxPro version |

Nullable value types use the same descriptor inference. This does not automatically add Visual FoxPro null flags or implement its null bitmap. For `decimal` with inferred version selection, use `[DbfNumeric(...)]` or `[DbfCurrency]` explicitly.

## Select the DBF version

`DbfCreateOptions.Version` defaults to `Unspecified`, which selects a supported version from the descriptors:

- Ordinary character, numeric, logical, and date fields select dBASE III (`DBase03`); a memo field selects `DBase83`.
- Float fields require dBASE IV (`DBase04`, or `DBase8B` with memo).
- Currency, binary date/time, integer, and null-flag fields require Visual FoxPro.
- `DbfInt32(AutoIncrement = true)` selects Visual FoxPro with auto-increment; variant/blob fields select its varchar variant.

Some descriptors, including the separate dBASE auto-increment type, double, timestamp, and memo-backed binary type, require an explicit version because inference cannot choose a safe one. A descriptor factory's existence is not a guarantee of complete support for that historical file format.

```csharp
using var table = Dbf.Create<Order>(
    "foxpro-orders.dbf",
    new DbfCreateOptions { Version = DbfVersion.VisualFoxPro });
```

Explicit version selection bypasses inference; match field widths and representations to the consuming application's format requirements. Auto-increment descriptors describe stored integer values; the library does not allocate IDs on append.
