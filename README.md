# dbase

A .NET library for reading and writing xBase DBF files.

## Features

- Read and write DBF files through untyped `DbfRecord` values or typed .NET records/classes
- Support implemented dBASE/FoxBASE/FoxPro/Visual FoxPro version markers exposed by `DbfVersion`
- Open sibling `.dbt` or `.fpt` memo files when present, and create memo files for schemas that require them
- Enumerate records and memo entries in file order
- Generate C# record/class definitions from existing DBF descriptors

## Installation

To install the library, add the following package reference to your project:

```xml
<PackageReference Include="DBase" Version="*" />
```

## Usage

### Reading a .dbf File

```cs
using DBase;

var dbfPath = "path/to/file.dbf";
using var dbf = Dbf.Open(dbfPath);

// Using built-in `DbfRecord` type.
foreach (var record in dbf.EnumerateRecords())
{
    foreach (var field in record)
    {
        Console.WriteLine(field);
    }

    Console.WriteLine();
}

// Using a custom type.
foreach (var record in dbf.EnumerateRecords<MyRecordType>())
{
    Console.WriteLine(record);
}

```

To override DBF text encoding, pass open options:

```cs
using System.Text;
using var dbf = Dbf.Open(
    dbfPath,
    new DbfOpenOptions { Encoding = Encoding.GetEncoding(1252) });
```

### Writing to a .dbf File

```cs
using DBase;

var dbfPath = "path/to/new-file.dbf";
using var dbf = Dbf.Create(
    dbfPath,
    [DbfFieldDescriptor.Character("FieldName", 20)]);

// Using built-in `DbfRecord` type.
dbf.Add(new DbfRecord("Value"));

// Using a custom type.
dbf.Add(new MyRecordType { FieldName = "Value" });
```

### Text Encoding

When opening a file by path, text encoding is resolved in this order:

1. `DbfOpenOptions.Encoding`
2. A sibling `.cpg` file
3. The DBF language-driver byte
4. `DbfOpenOptions.FallbackEncoding`

The built-in language mapping treats the vague OEM marker as code page 437 and the vague ANSI marker as
Windows-1252. The default fallback is code page 437.

Text writes for character and Visual FoxPro varchar fields are limited by the field byte length. If an
encoded value is too wide, it is truncated at a complete encoded character and any remaining bytes are
space-padded. Numeric, date, and binary fields remain strict about values that do not fit.

## References

- [Independent Software - dBASE DBF/DBT File Format](http://www.independent-software.com/dbase-dbf-dbt-file-format.html)
- [Manmrk - xBase Tutorials](http://www.manmrk.net/tutorials/database/xbase/)
- [Clicketyclick - xBase File Format](https://www.clicketyclick.dk/databases/xbase/format/index.html)
- [GitHub - infused/dbf](https://github.com/infused/dbf/tree/main/spec/fixtures)
- [FoxPro History](http://www.foxprohistory.org/)

## Contributing
Contributions are welcome! Please open an issue or submit a pull request on GitHub.

## License
This project is licensed under the MIT License. See the LICENSE file for details.
