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
    FoxBase = 0x02,

    /// <summary>
    /// FoxBase or dBASE III without memo fields.
    /// </summary>
    FoxBaseDBase3NoMemo = 0x03,

    /// <summary>
    /// dBASE IV without memo fields.
    /// </summary>
    DBase4NoMemo = 0x04,

    /// <summary>
    /// dBASE V without memo fields.
    /// </summary>
    DBase5NoMemo = 0x05,

    /// <summary>
    /// Visual FoxPro.
    /// </summary>
    VisualFoxPro = 0x30,

    /// <summary>
    /// Visual FoxPro with auto increment field.
    /// </summary>
    VisualFoxProWithAutoIncrement = 0x31,

    /// <summary>
    /// dBASE IV SQL table without memo fields.
    /// </summary>
    dBase4SQLTableNoMemo = 0x43,

    /// <summary>
    /// dBASE IV SQL system file without memo fields.
    /// </summary>
    dBase4SQLSystemNoMemo = 0x63,

    /// <summary>
    /// FoxBase or dBASE III with memo fields.
    /// </summary>
    FoxBaseDBase3WithMemo = 0x83,

    /// <summary>
    /// dBASE IV with memo fields.
    /// </summary>
    dBase4WithMemo = 0x8B,

    /// <summary>
    /// dBASE IV SQL table with memo fields.
    /// </summary>
    dBase4SQLTableWithMemo = 0xCB,

    /// <summary>
    /// FoxPro 2.x with memo fields.
    /// </summary>
    FoxPro2WithMemo = 0xF5,

    /// <summary>
    /// FoxBASE.
    /// </summary>
    FoxBASE = 0xFB
}
