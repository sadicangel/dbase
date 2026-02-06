namespace DBase;

/// <summary>
/// Defines the DBF field type code stored in the field descriptor (ASCII type byte).
/// </summary>
/// <remarks>
/// dBASE Level 7 extends the classic dBASE III/IV/5 set, and Visual FoxPro adds additional codes
/// (for example <c>Y</c>, <c>T</c>, <c>B</c>, <c>I</c>, <c>V</c>, <c>W</c>, <c>P</c>, <c>G</c>).
/// </remarks>
public enum DbfFieldType : byte
{
    /// <summary>
    /// dBASE Level 7 autoincrement (<c>+</c>): a signed 4-byte integer that increments automatically.
    /// </summary>
    AutoIncrement = (byte)'+',

    /// <summary>
    /// dBASE Level 7 binary memo pointer (<c>B</c>): stored as a 10-digit <c>.DBT</c> block number string.
    /// </summary>
    /// <remarks>
    /// Visual FoxPro uses <c>B</c> for Double; interpret this code in the context of the DBF version.
    /// </remarks>
    Binary = (byte)'B',

    /// <summary>
    /// Visual FoxPro BLOB (<c>W</c>): binary data stored in a memo (<c>.FPT</c>) file without code page translation.
    /// </summary>
    Blob = (byte)'W',

    /// <summary>
    /// Character text (<c>C</c>): OEM code page characters, space-padded to field width.
    /// </summary>
    Character = (byte)'C',

    /// <summary>
    /// Visual FoxPro currency (<c>Y</c>): stored as an 8-byte binary number.
    /// </summary>
    Currency = (byte)'Y',

    /// <summary>
    /// Date (<c>D</c>): stored as an 8-byte <c>YYYYMMDD</c> string.
    /// </summary>
    Date = (byte)'D',

    /// <summary>
    /// Visual FoxPro DateTime (<c>T</c>): 8-byte date/time value.
    /// </summary>
    DateTime = (byte)'T',

    /// <summary>
    /// dBASE Level 7 double (<c>O</c>): stored as an 8-byte IEEE double.
    /// </summary>
    Double = (byte)'O',

    /// <summary>
    /// Float (<c>F</c>): number stored as a right-justified string using <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c>.
    /// </summary>
    Float = (byte)'F',

    /// <summary>
    /// dBASE long integer (<c>I</c>): signed 4-byte integer.
    /// </summary>
    Int32 = (byte)'I',

    /// <summary>
    /// Logical (<c>L</c>): a single byte with <c>T</c>/<c>F</c> (space when uninitialized).
    /// </summary>
    Logical = (byte)'L',

    /// <summary>
    /// Memo (<c>M</c>): stored as a 10-digit <c>.DBT</c> block number string.
    /// </summary>
    Memo = (byte)'M',

    /// <summary>
    /// Visual FoxPro hidden system field for nullability flags (<c>0</c>, typically named <c>_NullFlags</c>).
    /// </summary>
    NullFlags = (byte)'0',

    /// <summary>
    /// Numeric (<c>N</c>): number stored as a right-justified string using <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c>.
    /// </summary>
    Numeric = (byte)'N',

    /// <summary>
    /// OLE object (<c>G</c>): stored as a 10-digit <c>.DBT</c> block number string.
    /// </summary>
    Ole = (byte)'G',

    /// <summary>
    /// Visual FoxPro picture (<c>P</c>).
    /// </summary>
    Picture = (byte)'P',

    /// <summary>
    /// dBASE Level 7 timestamp (<c>@</c>): two 32-bit integers (days since 01/01/4713 BC, and milliseconds since midnight).
    /// </summary>
    Timestamp = (byte)'@',

    /// <summary>
    /// Visual FoxPro varchar (<c>V</c>): variable-length character data with stored length.
    /// </summary>
    Variant = (byte)'V',
}
