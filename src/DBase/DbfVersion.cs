namespace DBase;

/// <summary>
/// Identifies the DBF file-format version marker stored in the header.
/// </summary>
public enum DbfVersion : byte
{
    /// <summary>
    /// Unspecified/unknown marker (<c>0x00</c>).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// FoxBASE / dBASE II style table (<c>0x02</c>).
    /// </summary>
    DBase02 = 0x02,

    /// <summary>
    /// FoxBASE or dBASE III table without memo fields (<c>0x03</c>).
    /// </summary>
    DBase03 = 0x03,

    /// <summary>
    /// dBASE IV table without memo fields (<c>0x04</c>).
    /// </summary>
    DBase04 = 0x04,

    /// <summary>
    /// dBASE V table without memo fields (<c>0x05</c>).
    /// </summary>
    DBase05 = 0x05,

    /// <summary>
    /// Visual FoxPro table (<c>0x30</c>).
    /// </summary>
    VisualFoxPro = 0x30,

    /// <summary>
    /// Visual FoxPro table with auto-increment support (<c>0x31</c>).
    /// </summary>
    VisualFoxProWithAutoIncrement = 0x31,

    /// <summary>
    /// Visual FoxPro table with <c>Varchar</c>/<c>Varbinary</c> support (<c>0x32</c>).
    /// </summary>
    VisualFoxProWithVarchar = 0x32,

    /// <summary>
    /// dBASE IV SQL table without memo fields (<c>0x43</c>).
    /// </summary>
    DBase43 = 0x43,

    /// <summary>
    /// dBASE IV SQL system file without memo fields (<c>0x63</c>).
    /// </summary>
    DBase63 = 0x63,

    /// <summary>
    /// FoxBASE or dBASE III table with memo fields (<c>0x83</c>).
    /// </summary>
    DBase83 = 0x83,

    /// <summary>
    /// dBASE IV table with memo fields (<c>0x8B</c>).
    /// </summary>
    DBase8B = 0x8B,

    /// <summary>
    /// dBASE IV SQL table with memo fields (<c>0xCB</c>).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    DBaseCB = 0xCB,

    /// <summary>
    /// FoxPro 2.x table with memo fields (<c>0xF5</c>).
    /// </summary>
    FoxPro2WithMemo = 0xF5,

    /// <summary>
    /// FoxBASE table marker (<c>0xFB</c>).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    FoxBASE = 0xFB
}
