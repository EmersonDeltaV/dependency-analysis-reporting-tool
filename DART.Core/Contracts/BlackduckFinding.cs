namespace DART.Core.Contracts;

public sealed class BlackduckFinding
{
    public string ApplicationName { get; init; } = string.Empty;

    public string SoftwareComponent { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string SecurityRisk { get; init; } = string.Empty;

    public string VulnerabilityId { get; init; } = string.Empty;

    public string RecommendedFix { get; init; } = string.Empty;

    public string MatchType { get; init; } = string.Empty;
}

