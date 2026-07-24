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
