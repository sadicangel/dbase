using System.Diagnostics.CodeAnalysis;

namespace DBase.Tests.DBase32;

// ReSharper disable once UnusedMember.Global
public class DBase32 : DBaseTest<DBase32Record>;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required for serialization")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Required for serialization")]
public record DBase32Record(
    string NAME,
    string _NullFlags);
