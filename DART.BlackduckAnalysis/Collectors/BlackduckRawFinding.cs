namespace DART.BlackduckAnalysis;

public sealed record BlackduckRawFinding(
    string ProjectId,
    string ProjectName,
    string ComponentOriginId,
    string SecurityRisk,
    string VulnerabilityId,
    string MatchType,
    string Version);
