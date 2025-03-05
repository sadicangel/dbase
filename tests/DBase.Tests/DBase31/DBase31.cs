namespace DBase.Tests.DBase31;
public class DBase31 : DBaseTest<DBase31Record>;

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
