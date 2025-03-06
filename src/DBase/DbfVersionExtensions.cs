namespace DBase;

/// <summary>
/// Extensions for <see cref="DbfVersion" />.
/// </summary>
public static class DbfVersionExtensions
{
    /// <summary>
    /// Determines whether the <see cref="DbfVersion"/> has memo fields.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool HasDosMemo(this DbfVersion version) =>
        ((byte)version & 0b0000_1000) != 0;

    /// <summary>
    /// Determines whether the <see cref="DbfVersion"/> has a SQL table.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool HasSqlTable(this DbfVersion version) =>
        ((byte)version & 0b0111_0000) != 0;

    /// <summary>
    /// Determines whether the <see cref="DbfVersion"/> has a FoxPro memo fields.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool HasDbtMemo(this DbfVersion version) =>
        ((byte)version & 0b1000_0000) != 0;

    /// <summary>
    /// Determines whether the <see cref="DbfVersion"/> is a FoxPro version.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool IsFoxPro(this DbfVersion version) =>
        version is DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.VisualFoxProWithVarchar;
}
