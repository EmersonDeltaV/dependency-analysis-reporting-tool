namespace DART.Core;

public sealed class EolFinding
{
    public string PackageId { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;

    public string CurrentVersion { get; init; } = string.Empty;

    public string VersionDate { get; init; } = string.Empty;

    public double AgeDays { get; init; }

    public string LatestVersion { get; init; } = string.Empty;

    public string LatestVersionDate { get; init; } = string.Empty;

    public string License { get; init; } = string.Empty;

    public string LicenseUrl { get; init; } = string.Empty;

    public string RecommendedAction { get; init; } = string.Empty;
}

