namespace DBase;

/// <summary>
/// Represents the type of memo record.
/// </summary>
public enum MemoRecordType
{
    /// <summary>
    /// Used for images, OLE objects (FoxPro)
    /// </summary>
    Picture = 0x00,

    /// <summary>
    /// Standard text memo (dBASE, FoxPro)
    /// </summary>
    Memo = 0x01,

    /// <summary>
    /// FoxPro-specific object storage
    /// </summary>
    Object = 0x02,
}
