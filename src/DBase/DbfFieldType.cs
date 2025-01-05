namespace DBase;

/// <summary>
/// Defines the field type of a <see cref="Dbf" />.
/// </summary>
public enum DbfFieldType : byte
{
    /// <summary>
    /// Automatically incremented <see cref="int" /> values.
    /// </summary>
    /// <remarks>
    /// Deleting a row does not change the field values of other rows. Be aware that adding an auto increment field will pack the table.
    /// </remarks>
    AutoIncrement = (byte)'+',
    /// <summary>
    /// Like <see cref="Memo" /> fields, but not for text processing.
    /// </summary>
    Binary = (byte)'B',
    /// <summary>
    /// Like <see cref="Memo" /> fields, but optimized for binary data.
    /// </summary>
    Blob = (byte)'W',
    /// <summary>
    /// Any ASCII text (padded with spaces).
    /// </summary>
    Character = (byte)'C',
    /// <summary>
    /// Currency values (stored internally as 8-byte BCD, binary-coded decimal).
    /// </summary>
    Currency = (byte)'Y',
    /// <summary>
    /// Numbers and a character to separate month, day, and year (stored internally as 8 digits in YYYYMMDD format).
    /// </summary>
    Date = (byte)'D',
    /// <summary>
    /// Date/Time value (stored internally as 8 bytes). The first 4 bytes are a 32-bit little-endian
    /// integer representation of the Julian date, where Oct. 15, 1582 = 2299161 per www.nr.com/julian.html
    /// The last 4 bytes are a 32-bit little-endian integer time of day represented as milliseconds since midnight.
    /// </summary>
    DateTime = (byte)'T',
    /// <summary>
    /// <see cref="double" />. Optimized for speed.
    /// </summary>
    Double = (byte)'O',
    /// <summary>
    /// <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c> (right justified, padded with whitespace).
    /// </summary>
    Float = (byte)'F',
    /// <summary>
    /// <see cref="int" />. Optimized for speed.
    /// </summary>
    Int32 = (byte)'I',
    /// <summary>
    /// <c>Y</c>, <c>y</c>, <c>N</c>, <c>n</c>, <c>T</c>, <c>t</c>, <c>F</c>, <c>f</c>, <c>1</c>, <c>0</c> or <c>?</c> (when uninitialized).
    /// </summary>
    /// <remarks>
    /// In some cases, '<c>?</c>' can be replaced with '<c> </c>' (space character <c>0x20</c>).
    /// </remarks>
    Logical = (byte)'L',
    /// <summary>
    /// Any ASCII text (stored internally as 10 digits representing a .dbt block number, right justified, padded with whitespace).
    /// </summary>
    Memo = (byte)'M',
    /// <summary>
    /// Used for '_NullFlags' in Visual FoxPro. 
    /// </summary>
    NullFlags = (byte)'0',
    /// <summary>
    /// <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c> (right justified, padded with whitespace).
    /// </summary>
    Numeric = (byte)'N',
    /// <summary>
    /// OLE Objects in MS Windows versions.
    /// </summary>
    Ole = (byte)'G',
    /// <summary>
    /// Picture in Fox Pro versions.
    /// </summary>
    Picture = (byte)'P',
    /// <summary>
    /// Date/Time stamp, including the Date format plus hours, minutes, and seconds, such as hh:MM:ss.
    /// </summary>
    Timestamp = (byte)'@',
    /// <summary>
    /// Visual Fox Pro varchar or varbinary.
    /// </summary>
    Variant = (byte)'V',
}
