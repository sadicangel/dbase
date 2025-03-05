namespace DBase.Tests.DBase32;
public class DBase32 : DBaseTest<DBase32Record>;

public record DBase32Record(
    string NAME,
    string _NullFlags);
