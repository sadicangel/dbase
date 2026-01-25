namespace DBase;

/// <summary>
/// Represents the version of a DBF file.
/// </summary>
public enum DbfVersion : byte
{
    /// <summary>
    /// Unknown version.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// FoxBase version.
    /// </summary>
    DBase02 = 0x02,

    /// <summary>
    /// FoxBase or dBASE III without memo fields.
    /// </summary>
    DBase03 = 0x03,

    /// <summary>
    /// dBASE IV without memo fields.
    /// </summary>
    DBase04 = 0x04,

    /// <summary>
    /// dBASE V without memo fields.
    /// </summary>
    DBase05 = 0x05,

    /// <summary>
    /// Visual FoxPro.
    /// </summary>
    VisualFoxPro = 0x30,

    /// <summary>
    /// Visual FoxPro with auto increment field.
    /// </summary>
    VisualFoxProWithAutoIncrement = 0x31,

    /// <summary>
    /// Visual FoxPro with field type Varchar or Varbinary.
    /// </summary>
    VisualFoxProWithVarchar = 0x32,

    /// <summary>
    /// dBASE IV SQL table without memo fields.
    /// </summary>
    DBase43 = 0x43,

    /// <summary>
    /// dBASE IV SQL system file without memo fields.
    /// </summary>
    DBase63 = 0x63,

    /// <summary>
    /// FoxBase or dBASE III with memo fields.
    /// </summary>
    DBase83 = 0x83,

    /// <summary>
    /// dBASE IV with memo fields.
    /// </summary>
    DBase8B = 0x8B,

    /// <summary>
    /// dBASE IV SQL table with memo fields.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    DBaseCB = 0xCB,

    /// <summary>
    /// FoxPro 2.x with memo fields.
    /// </summary>
    FoxPro2WithMemo = 0xF5,

    /// <summary>
    /// FoxBASE.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    FoxBASE = 0xFB
}
