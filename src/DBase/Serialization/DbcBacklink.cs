using System.Runtime.CompilerServices;
using System.Text;

namespace DBase.Serialization;

/// <summary>
/// Represents the Visual FoxPro database container (DBC) backlink area stored in a DBF file header.
/// </summary>
/// <remarks>
/// <para>
/// In Visual FoxPro–style DBF files (versions 0x30, 0x31, 0x32), a table may optionally be associated with
/// a database container (<c>.dbc</c> file). When present, the DBF header reserves a 263-byte <em>backlink</em>
/// area immediately after the field descriptor terminator (<c>0x0D</c>) and before the first data record.
/// </para>
/// <para>
/// The backlink area is FoxPro-specific and not formally specified; it should be treated as an opaque blob.
/// Many files store one or more null-terminated strings within this area (often including the DBC path and
/// the table name as registered in the database container), but this is not guaranteed for all producers.
/// </para>
/// </remarks>
[InlineArray(263)]
public struct DbcBacklink
{
    private byte _e0;

    /// <summary>
    /// Represents an empty backlink is present.
    /// </summary>
    public static readonly DbcBacklink Empty = default;

    /// <summary>
    /// Gets the database container path (<c>.dbc</c>) decoded from the backlink area, if available.
    /// </summary>
    /// <remarks>
    /// This value is extracted on a best-effort basis from the backlink data and may be <see langword="null" />
    /// if the table is not bound to a database container or if the content is not in the expected form.
    /// </remarks>
    public string? DbcPath => GetNullTerminatedStringAtOrDefault(0);

    /// <summary>
    /// Gets the logical table name decoded from the backlink area, if available.
    /// </summary>
    /// <remarks>
    /// This value is extracted on a best-effort basis from the backlink data and may be <see langword="null" />
    /// if the content is not in the expected form.
    /// </remarks>
    public string? TableName => GetNullTerminatedStringAtOrDefault(1);

    private string? GetNullTerminatedStringAtOrDefault(int index)
    {
        Span<byte> span = this;
        var i = 0;
        using var enumerator = span.Split((byte)0);
        do
        {
            if (!enumerator.MoveNext())
                return null;
        } while (i++ < index);

        return Encoding.UTF8.GetString(span[enumerator.Current]);
    }
}
