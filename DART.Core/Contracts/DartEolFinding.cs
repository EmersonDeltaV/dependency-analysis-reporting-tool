namespace DART.Core.Contracts;

public sealed class DartEolFinding
{
    public string PackageId { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;

    public string CurrentVersion { get; init; } = string.Empty;

    public string LatestVersion { get; init; } = string.Empty;

    public string RecommendedAction { get; init; } = string.Empty;
}
