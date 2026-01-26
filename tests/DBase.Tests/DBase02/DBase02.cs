using System.Diagnostics.CodeAnalysis;

namespace DBase.Tests.DBase02;

// ReSharper disable once UnusedMember.Global
public class DBase02 : DBaseTest<DBase02Record>;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for serialization")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Required for serialization")]
public sealed record DBase02Record(
    double? EMP_NMBR,
    string LAST,
    string FIRST,
    string ADDR,
    string CITY,
    string ZIP_CODE,
    string PHONE,
    string SSN,
    string HIREDATE,
    string TERMDATE,
    string CLASS,
    string DEPT,
    double? PAYRATE,
    double? START_PAY);
