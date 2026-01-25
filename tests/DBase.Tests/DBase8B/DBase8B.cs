using System.Diagnostics.CodeAnalysis;

namespace DBase.Tests.DBase8b;

// ReSharper disable once UnusedMember.Global
public class DBase8B : DBaseTest<DBase8BRecord>;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for serialization")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Required for serialization")]
public record DBase8BRecord(
    string CHARACTER,
    double? NUMERICAL,
    DateTime? DATE,
    bool? LOGICAL,
    double? FLOAT,
    string MEMO);
