namespace DBase.Tests.DBase8b;
public class DBase8B : DBaseTest<DBase8BRecord>;

public record DBase8BRecord(
    string CHARACTER,
    double? NUMERICAL,
    DateTime? DATE,
    bool? LOGICAL,
    double? FLOAT,
    string MEMO);
