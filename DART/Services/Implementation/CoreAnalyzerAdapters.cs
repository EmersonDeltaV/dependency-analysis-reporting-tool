using DART.BlackduckAnalysis;
using DART.Core;
using DART.Core.Blackduck;
using DART.EOLAnalysis;
using DART.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DART.Services.Implementation;

public sealed class BlackduckAnalyzerAdapter : IBlackduckAnalyzer
{
    private readonly IBlackduckReportGenerator _blackduckReportGenerator;
    private readonly IBlackduckApiService _blackduckApiService;
    private readonly IBlackduckFindingCollector _collector;
    private readonly DART.Core.Config _config;
    private readonly ILogger<BlackduckAnalyzerAdapter> _logger;

    public BlackduckAnalyzerAdapter(
        IBlackduckReportGenerator blackduckReportGenerator,
        IBlackduckApiService blackduckApiService,
        IOptions<DART.Core.Config> configOptions,
        IBlackduckFindingCollector collector,
        ILogger<BlackduckAnalyzerAdapter> logger)
    {
        _blackduckReportGenerator = blackduckReportGenerator;
        _blackduckApiService = blackduckApiService;
        _collector = collector;
        _config = configOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<BlackduckFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (!request.EnableBlackduckAnalysis)
        {
            return Array.Empty<BlackduckFinding>();
        }

        _blackduckReportGenerator.SetRuntimeConfig(_config.BlackduckConfiguration, _config.ReportConfiguration.OutputFilePath);

        try
        {
            await _blackduckReportGenerator.GenerateReport();
            return await CollectFindingsAsync(cancellationToken);
        }
        finally
        {
            await _blackduckReportGenerator.Cleanup();
        }
    }

    private async Task<IReadOnlyCollection<BlackduckFinding>> CollectFindingsAsync(CancellationToken cancellationToken)
    {
        var csvFiles = GetCsvFiles();
        if (csvFiles.Length == 0)
        {
            return Array.Empty<BlackduckFinding>();
        }

        var latestVersions = await _blackduckApiService.GetLatestProjectVersion(_config.BlackduckConfiguration);
        var configuredVersions = _config.BlackduckConfiguration.BlackduckRepositories
            .ToDictionary(repository => repository.Id, repository => repository.Versions);

        var options = new BlackduckCollectorOptions
        {
            IncludeTransitiveDependency = _config.BlackduckConfiguration.IncludeTransitiveDependency,
            IncludeRecommendedFix = _config.BlackduckConfiguration.IncludeRecommendedFix,
            LatestVersions = latestVersions,
            ConfiguredVersions = configuredVersions
        };

        var findings = new List<BlackduckFinding>();

        foreach (var csvFile in csvFiles)
        {
            var csvData = await File.ReadAllLinesAsync(csvFile, cancellationToken);
            if (csvData.Length == 0)
            {
                continue;
            }

            var headerIndexes = GetHeaderIndexes(csvData[0].Split(','));
            if (headerIndexes is null)
            {
                _logger.LogError("Required columns not found in CSV file {CsvFile}.", csvFile);
                continue;
            }

            var rows = new List<BlackduckRawFinding>();
            for (var index = 1; index < csvData.Length; index++)
            {
                var row = ParseRow(csvData[index], headerIndexes);
                if (row is not null)
                {
                    rows.Add(row);
                }
            }

            if (rows.Count == 0)
            {
                continue;
            }

            var collected = await _collector.CollectAsync(
                rows,
                options,
                (vulnerabilityId, ct) => _blackduckApiService.GetRecommendedFix(_config.BlackduckConfiguration, vulnerabilityId),
                cancellationToken);

            findings.AddRange(collected);
        }

        return findings;
    }

    private string[] GetCsvFiles()
    {
        var reportFolderPath = Path.Combine(_config.ReportConfiguration.OutputFilePath, BlackduckConfiguration.DownloadsFolderName);
        if (!Directory.Exists(reportFolderPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(reportFolderPath, "*.csv", SearchOption.AllDirectories);
    }

    private static HeaderIndexes? GetHeaderIndexes(string[] headers)
    {
        var indexes = new HeaderIndexes
        {
            ProjectId = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.ProjectId, StringComparison.OrdinalIgnoreCase)),
            ProjectName = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.ProjectName, StringComparison.OrdinalIgnoreCase)),
            ComponentOriginId = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.ComponentOriginId, StringComparison.OrdinalIgnoreCase)),
            SecurityRisk = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.SecurityRisk, StringComparison.OrdinalIgnoreCase)),
            VulnerabilityId = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.VulnerabilityId, StringComparison.OrdinalIgnoreCase)),
            MatchType = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.MatchType, StringComparison.OrdinalIgnoreCase)),
            Version = Array.FindIndex(headers, value => value.Equals(BlackduckCSVHeaders.Version, StringComparison.OrdinalIgnoreCase))
        };

        return indexes.IsValid ? indexes : null;
    }

    private static BlackduckRawFinding? ParseRow(string row, HeaderIndexes indexes)
    {
        var parsed = ParseCsvRow(row);
        var maxIndex = Math.Max(
            indexes.Version,
            Math.Max(
                indexes.MatchType,
                Math.Max(
                    indexes.VulnerabilityId,
                    Math.Max(indexes.ComponentOriginId, Math.Max(indexes.ProjectName, indexes.ProjectId)))));

        if (parsed.Length <= maxIndex)
        {
            return null;
        }

        return new BlackduckRawFinding(
            parsed[indexes.ProjectId],
            parsed[indexes.ProjectName],
            parsed[indexes.ComponentOriginId],
            parsed[indexes.SecurityRisk],
            parsed[indexes.VulnerabilityId],
            parsed[indexes.MatchType],
            parsed[indexes.Version]);
    }

    private static string[] ParseCsvRow(string row)
    {
        var pattern = "(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)";
        var matches = Regex.Matches(row, pattern);
        var values = new List<string>(matches.Count);

        foreach (Match match in matches)
        {
            values.Add(match.Value.Trim('"'));
        }

        return values.ToArray();
    }

    private sealed class HeaderIndexes
    {
        public int ProjectId { get; init; }
        public int ProjectName { get; init; }
        public int ComponentOriginId { get; init; }
        public int SecurityRisk { get; init; }
        public int VulnerabilityId { get; init; }
        public int MatchType { get; init; }
        public int Version { get; init; }

        public bool IsValid =>
            ProjectId >= 0 &&
            ProjectName >= 0 &&
            ComponentOriginId >= 0 &&
            SecurityRisk >= 0 &&
            VulnerabilityId >= 0 &&
            MatchType >= 0 &&
            Version >= 0;
    }
}

public sealed class EolAnalyzerAdapter : IEolAnalyzer
{
    private readonly IEOLAnalysisService _eolAnalysisService;
    private readonly DART.Core.Config _config;

    public EolAnalyzerAdapter(IEOLAnalysisService eolAnalysisService, IOptions<DART.Core.Config> configOptions)
    {
        _eolAnalysisService = eolAnalysisService;
        _config = configOptions.Value;
    }

    public async Task<IReadOnlyCollection<EolFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var eolConfig = _config.EOLAnalysis;
        if (!request.EnableEolAnalysis || eolConfig.Repositories.Count == 0)
        {
            return Array.Empty<EolFinding>();
        }

        var results = await _eolAnalysisService.AnalyzeRepositoriesAsync(eolConfig, _config.FeatureToggles, cancellationToken);
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

