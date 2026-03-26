using System.Text.RegularExpressions;
using DART.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DART.BlackduckAnalysis;

public sealed class BlackduckAnalyzerAdapter : IBlackduckAnalyzer
{
    private readonly IBlackduckReportGenerator _blackduckReportGenerator;
    private readonly IBlackduckApiService _blackduckApiService;
    private readonly IBlackduckFindingCollector _collector;
    private readonly BlackduckConfiguration _blackduckConfiguration;
    private readonly ReportConfiguration _reportConfiguration;
    private readonly ILogger<BlackduckAnalyzerAdapter> _logger;

    public BlackduckAnalyzerAdapter(
        IBlackduckReportGenerator blackduckReportGenerator,
        IBlackduckApiService blackduckApiService,
        IOptions<BlackduckConfiguration> blackduckOptions,
        IOptions<ReportConfiguration> reportOptions,
        IBlackduckFindingCollector collector,
        ILogger<BlackduckAnalyzerAdapter> logger)
    {
        _blackduckReportGenerator = blackduckReportGenerator;
        _blackduckApiService = blackduckApiService;
        _collector = collector;
        _blackduckConfiguration = blackduckOptions.Value;
        _reportConfiguration = reportOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<BlackduckFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        if (!request.EnableBlackduckAnalysis)
        {
            return Array.Empty<BlackduckFinding>();
        }

        _blackduckReportGenerator.SetRuntimeConfig(_blackduckConfiguration, _reportConfiguration.OutputFilePath);

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

        var latestVersions = await _blackduckApiService.GetLatestProjectVersion(_blackduckConfiguration);
        var configuredVersions = _blackduckConfiguration.BlackduckRepositories
            .ToDictionary(repository => repository.Id, repository => repository.Versions);

        var options = new BlackduckCollectorOptions
        {
            IncludeTransitiveDependency = _blackduckConfiguration.IncludeTransitiveDependency,
            IncludeRecommendedFix = _blackduckConfiguration.IncludeRecommendedFix,
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
                (vulnerabilityId, ct) => _blackduckApiService.GetRecommendedFix(_blackduckConfiguration, vulnerabilityId),
                cancellationToken);

            findings.AddRange(collected.Select(MapToCoreFinding));
        }

        return findings;
    }

    private string[] GetCsvFiles()
    {
        var reportFolderPath = Path.Combine(_reportConfiguration.OutputFilePath, BlackduckConfiguration.DownloadsFolderName);
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
            ProjectId = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.ProjectId, StringComparison.OrdinalIgnoreCase)),
            ProjectName = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.ProjectName, StringComparison.OrdinalIgnoreCase)),
            ComponentOriginId = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.ComponentOriginId, StringComparison.OrdinalIgnoreCase)),
            SecurityRisk = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.SecurityRisk, StringComparison.OrdinalIgnoreCase)),
            VulnerabilityId = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.VulnerabilityId, StringComparison.OrdinalIgnoreCase)),
            MatchType = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.MatchType, StringComparison.OrdinalIgnoreCase)),
            Version = Array.FindIndex(headers, value => value.Equals(BlackduckCsvHeaders.Version, StringComparison.OrdinalIgnoreCase))
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

    private static BlackduckFinding MapToCoreFinding(BlackduckCollectedFinding finding)
    {
        return new BlackduckFinding
        {
            ApplicationName = finding.ApplicationName,
            SoftwareComponent = finding.SoftwareComponent,
            Version = finding.Version,
            SecurityRisk = finding.SecurityRisk,
            VulnerabilityId = finding.VulnerabilityId,
            RecommendedFix = finding.RecommendedFix,
            MatchType = finding.MatchType
        };
    }

    private static class BlackduckCsvHeaders
    {
        public const string ProjectId = "Project id";
        public const string ProjectName = "Project name";
        public const string ComponentOriginId = "Component origin id";
        public const string SecurityRisk = "Security Risk";
        public const string VulnerabilityId = "Vulnerability ID";
        public const string MatchType = "Match type";
        public const string Version = "Version";
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
