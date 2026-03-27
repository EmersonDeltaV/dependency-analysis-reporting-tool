namespace DART.BlackduckAnalysis;

public sealed class BlackduckCollectorOptions
{
    public bool IncludeTransitiveDependency { get; init; }

    public bool IncludeRecommendedFix { get; init; }

    public IReadOnlyDictionary<string, string> LatestVersions { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> ConfiguredVersions { get; init; } = new Dictionary<string, string>();
}
