using System.Diagnostics.CodeAnalysis;

namespace DBase.Tests.DBase31;

// ReSharper disable once UnusedMember.Global
public class DBase31 : DBaseTest<DBase31Record>;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for serialization")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Required for serialization")]
public record DBase31Record(
    int PRODUCTID,
    string PRODUCTNAM,
    int SUPPLIERID,
    int CATEGORYID,
    string QUANTITYPE,
    decimal UNITPRICE,
    int UNITSINSTO,
    int UNITSONORD,
    int REORDERLEV,
    bool? DISCONTINU,
    string _NullFlags);
