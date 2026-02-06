namespace DBase;

/// <summary>
/// Defines memo-record type markers used by FoxPro-style memo blocks.
/// </summary>
public enum MemoRecordType
{
    /// <summary>
    /// Picture/OLE-style binary payload.
    /// </summary>
    Picture = 0x00,

    /// <summary>
    /// Standard memo payload.
    /// </summary>
    Memo = 0x01,

    /// <summary>
    /// FoxPro object payload.
    /// </summary>
    Object = 0x02,
}
