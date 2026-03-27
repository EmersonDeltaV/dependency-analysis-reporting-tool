namespace DART.BlackduckAnalysis;

public sealed class BlackduckFindingCollector : IBlackduckFindingCollector
{
    public async Task<IReadOnlyList<BlackduckCollectedFinding>> CollectAsync(
        IReadOnlyCollection<BlackduckRawFinding> rows,
        BlackduckCollectorOptions options,
        Func<string, CancellationToken, Task<string>> recommendedFixResolver,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<BlackduckCollectedFinding>();

        foreach (var row in rows)
        {
            if (!options.IncludeTransitiveDependency &&
                row.MatchType.Equals("Transitive Dependency", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var requestedVersions = options.ConfiguredVersions.TryGetValue(row.ProjectId, out var versions)
                ? versions.Split(',').Select(v => v.Trim().ToLowerInvariant()).ToArray()
                : [];

            var latestVersion = options.LatestVersions.TryGetValue(row.ProjectId, out var latest)
                ? latest
                : string.Empty;

            var rowVersion = row.Version.ToLowerInvariant();
            var shouldInclude = requestedVersions.Contains(string.Empty)
                || requestedVersions.Contains(rowVersion, StringComparer.OrdinalIgnoreCase)
                || (requestedVersions.Contains("<latest>") &&
                    row.Version.Equals(latestVersion, StringComparison.OrdinalIgnoreCase));

            if (!shouldInclude)
            {
                continue;
            }

            var finding = new BlackduckCollectedFinding
            {
                ApplicationName = row.ProjectName,
                SoftwareComponent = row.ComponentOriginId,
                SecurityRisk = row.SecurityRisk,
                VulnerabilityId = row.VulnerabilityId,
                MatchType = row.MatchType,
                Version = row.Version,
                RecommendedFix = options.IncludeRecommendedFix
                    ? await recommendedFixResolver(row.VulnerabilityId, cancellationToken)
                    : "N/A"
            };

            findings.Add(finding);
        }

        return findings;
    }
}
