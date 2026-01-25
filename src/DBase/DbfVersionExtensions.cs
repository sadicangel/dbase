namespace DBase;

/// <summary>
/// Extensions for <see cref="DbfVersion" />.
/// </summary>
public static class DbfVersionExtensions
{
    /// <param name="version"></param>
    extension(DbfVersion version)
    {
        /// <summary>
        /// Determines whether the <see cref="DbfVersion"/> has memo fields.
        /// </summary>
        /// <returns></returns>
        public bool HasDosMemo() =>
            ((byte)version & 0b0000_1000) != 0;

        /// <summary>
        /// Determines whether the <see cref="DbfVersion"/> has a SQL table.
        /// </summary>
        /// <returns></returns>
        public bool HasSqlTable() =>
            ((byte)version & 0b0111_0000) != 0;

        /// <summary>
        /// Determines whether the <see cref="DbfVersion"/> has a FoxPro memo fields.
        /// </summary>
        /// <returns></returns>
        public bool HasDbtMemo() =>
            ((byte)version & 0b1000_0000) != 0;

        /// <summary>
        /// Determines whether the <see cref="DbfVersion"/> is a FoxPro version.
        /// </summary>
        /// <returns></returns>
        public bool IsFoxPro() =>
            version is DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.VisualFoxProWithVarchar;
    }
}
