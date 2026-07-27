using System.Text;

namespace DBase;

/// <summary>
/// Configures how a new DBF table is created.
/// </summary>
public sealed class DbfCreateOptions
{
    /// <summary>
    /// Gets the DBF version marker written to the table header.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DbfVersion.Unspecified"/> to infer the smallest supported DBF version from the field
    /// descriptors.
    /// </remarks>
    public DbfVersion Version { get; init; } = DbfVersion.Unspecified;

    /// <summary>
    /// Gets the language-driver marker written to the table header.
    /// </summary>
    /// <remarks>
    /// If <see cref="Encoding"/> is not set, this marker is also used to choose the text encoding for the
    /// created table.
    /// </remarks>
    public DbfLanguage Language { get; init; } = DbfLanguage.Ansi;

    /// <summary>
    /// Gets the text encoding used to write character, varchar, and memo text fields.
    /// </summary>
    /// <remarks>
    /// Set this value when the desired text encoding does not match the DBF language-driver marker, or when
    /// the marker is not specific enough for the source data.
    /// </remarks>
    public Encoding? Encoding { get; init; }
}
