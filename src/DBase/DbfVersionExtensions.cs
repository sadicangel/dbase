namespace DBase;

/// <summary>
/// Extensions for <see cref="DbfVersion" />.
/// </summary>
public static class DbfVersionExtensions
{
    /// <param name="version">The version marker to inspect.</param>
    extension(DbfVersion version)
    {
        /// <summary>
        /// Determines whether the dBASE memo bit (<c>0x08</c>) is set.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the memo flag bit is set for <paramref name="version"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool HasDosMemo() =>
            ((byte)version & 0b0000_1000) != 0;

        /// <summary>
        /// Determines whether the SQL-table marker bits (<c>0x70</c>) are set.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when any SQL marker bit is set for <paramref name="version"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool HasSqlTable() =>
            ((byte)version & 0b0111_0000) != 0;

        /// <summary>
        /// Determines whether the high bit (<c>0x80</c>) is set.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the high-bit marker is set for <paramref name="version"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool HasDbtMemo() =>
            ((byte)version & 0b1000_0000) != 0;

        /// <summary>
        /// Determines whether the <see cref="DbfVersion"/> is a FoxPro version.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> for Visual FoxPro variants supported by this library; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public bool IsFoxPro() =>
            version is DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.VisualFoxProWithVarchar;
    }
}
