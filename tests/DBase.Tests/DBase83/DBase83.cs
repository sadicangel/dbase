using System.Diagnostics.CodeAnalysis;

namespace DBase.Tests.DBase83;

// ReSharper disable once UnusedMember.Global
public class DBase83 : DBaseTest<DBase83Record>;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for serialization")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Required for serialization")]
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
