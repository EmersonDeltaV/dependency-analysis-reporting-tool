using DART.Core;
using Microsoft.Extensions.Options;

namespace DART.EOLAnalysis;

public sealed class EolAnalyzerAdapter : IEolAnalyzer
{
    private readonly IEOLAnalysisService _eolAnalysisService;
    private readonly EOLAnalysisConfig _eolConfig;
    private readonly FeatureToggles _featureToggles;

    public EolAnalyzerAdapter(
        IEOLAnalysisService eolAnalysisService,
        IOptions<EOLAnalysisConfig> eolConfigOptions,
        IOptions<FeatureToggles> featureTogglesOptions)
    {
        _eolAnalysisService = eolAnalysisService;
        _eolConfig = eolConfigOptions.Value;
        _featureToggles = featureTogglesOptions.Value;
    }

    public async Task<IReadOnlyCollection<EolFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (!request.EnableEolAnalysis || _eolConfig.Repositories.Count == 0)
        {
            return Array.Empty<EolFinding>();
        }

        var eolToggles = new EolFeatureToggles
        {
            EnableCSharpAnalysis = _featureToggles.EnableCSharpAnalysis,
            EnableNpmAnalysis = _featureToggles.EnableNpmAnalysis,
            IncludeNpmDevDependencies = _featureToggles.IncludeNpmDevDependencies
        };

        var results = await _eolAnalysisService.AnalyzeRepositoriesAsync(_eolConfig, eolToggles, cancellationToken);
        return results.Select(item => new EolFinding
        {
            PackageId = item.Id,
            Repository = item.Repository,
            Project = item.Project,
            CurrentVersion = item.Version,
            VersionDate = item.VersionDate,
            AgeDays = item.Age,
            LatestVersion = item.LatestVersion,
            LatestVersionDate = item.LatestVersionDate,
            License = item.License,
            LicenseUrl = item.LicenseUrl,
            RecommendedAction = item.Action
        }).ToList();
    }
}
