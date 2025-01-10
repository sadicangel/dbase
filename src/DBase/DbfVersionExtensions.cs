namespace DBase;

public static class DbfVersionExtensions
{
    public static bool HasDosMemo(this DbfVersion version) =>
        ((byte)version & 0b0000_1000) != 0;

    public static bool HasSqlTable(this DbfVersion version) =>
        ((byte)version & 0b0111_0000) != 0;

    public static bool HasDbtMemo(this DbfVersion version) =>
        ((byte)version & 0b1000_0000) != 0;

    public static bool IsFoxPro(this DbfVersion version) =>
        version is DbfVersion.VisualFoxPro or DbfVersion.VisualFoxProWithAutoIncrement or DbfVersion.VisualFoxProWithVarchar;
}
