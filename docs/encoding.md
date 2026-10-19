---
title: Text encoding
nav_order: 5
---

# Text encoding

When opening a table by path, DBase chooses character, varchar, and memo-text encoding in this order:

1. `DbfOpenOptions.Encoding`, when explicitly set.
2. A sibling `.cpg` file, when it identifies a supported encoding.
3. The DBF language-driver byte.
4. `DbfOpenOptions.FallbackEncoding`, which defaults to code page 437.

A `.cpg` file can contain a code page such as `1252` or `CP1252`, a supported encoding name such as `UTF-8`, or the aliases `ANSI` and `OEM`. Whitespace and a byte-order mark are ignored. Unrecognized sidecar contents fall through to the language marker.

The ambiguous `DbfLanguage.Oem` marker maps to code page 437, and `DbfLanguage.Ansi` maps to Windows-1252. Neither proves the actual encoding of an external file; override it when the data producer specifies a different encoding.

```csharp
using System.Text;
using DBase;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
using var table = Dbf.Open(
    "people.dbf",
    new DbfOpenOptions { Encoding = Encoding.GetEncoding(1252) });
Console.WriteLine(table.Encoding.WebName);
```

DBase registers the code-page provider internally. Register it yourself before resolving a legacy code page in application code, as in the example above.

## Writing text

`DbfCreateOptions.Language` controls the header marker and defaults to `Ansi`. Set `Encoding` to choose different output bytes; the explicit encoding does not update the language marker or write a `.cpg` file automatically.

```csharp
using var table = Dbf.Create(
    "utf8.dbf",
    [DbfFieldDescriptor.Character("NAME", 30)],
    new DbfCreateOptions { Encoding = Encoding.UTF8 });
table.Add("João");
File.WriteAllText("utf8.cpg", "UTF-8");
```

Character fields are space-padded and trimmed on read. Text that exceeds the field's byte width is truncated at a complete encoded character. Varchar storage reserves its last byte for the encoded length. Numeric, date, and binary values must fit their representation; those failures are reported instead of text-style truncation.

Numeric formatting uses the decimal separator selected from the DBF language marker, rather than the current process culture or the encoding override. Inspect `Dbf.DecimalSeparator` when exchanging numeric text with another application.
