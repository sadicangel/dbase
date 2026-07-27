using System.Text;

namespace DBase;

/// <summary>
/// Configures how an existing DBF table is opened.
/// </summary>
public sealed class DbfOpenOptions
{
    private Encoding _fallbackEncoding = DbfEncoding.DefaultFallbackEncoding;

    /// <summary>
    /// Gets the text encoding used to read character, varchar, and memo text fields.
    /// </summary>
    /// <remarks>
    /// When this value is set, it overrides both a sibling <c>.cpg</c> file and the DBF language-driver
    /// marker stored in the table header.
    /// </remarks>
    public Encoding? Encoding { get; init; }

    /// <summary>
    /// Gets the text encoding used when neither <see cref="Encoding"/>, a sibling <c>.cpg</c> file, nor
    /// the DBF language-driver marker identifies a supported encoding.
    /// </summary>
    /// <remarks>
    /// The default fallback is code page 437, matching the library's practical default for an unspecified
    /// OEM DBF marker.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Encoding FallbackEncoding
    {
        get => _fallbackEncoding;
        init => _fallbackEncoding = value ?? throw new ArgumentNullException(nameof(value));
    }
}
