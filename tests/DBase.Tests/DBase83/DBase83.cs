namespace DBase.Tests.DBase83;

public class DBase83 : DBaseTest<DBase83Record>;

public record DBase83Record(
    double? ID,
    double? CATCOUNT,
    double? AGRPCOUNT,
    double? PGRPCOUNT,
    double? ORDER,
    string CODE,
    string NAME,
    string THUMBNAIL,
    string IMAGE,
    double? PRICE,
    double? COST,
    string DESC,
    double? WEIGHT,
    bool? TAXABLE,
    bool? ACTIVE);
