using DART.BlackduckAnalysis;
using DART.Core;
using DART.Core.Blackduck;
using DART.Exceptions;
using DART.Models;
using DART.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using CsvRowDetails = DART.Models.RowDetails;

namespace DART.Services.Implementation
{
    public class CsvService : ICsvService
    {
        private readonly IBlackduckApiService _blackduckApiService;
        private readonly IBlackduckFindingCollector _blackduckFindingCollector;
        private readonly IExcelService _excelService;
        private readonly ILogger<CsvService> _logger;
        private readonly DART.Core.Config _config;

        private int projectIdIndex;
        private int projectNameIndex;
        private int componentOriginIdIndex;
        private int securityRiskIndex;
        private int vulnerabilityIdIndex;
        private int matchTypeIndex;
        private int versionIndex;

        public CsvService(IBlackduckApiService blackduckApiService,
                          IExcelService excelService,
                          IConfiguration configuration,
                          ILogger<CsvService> logger,
                          IBlackduckFindingCollector? blackduckFindingCollector = null)
        {
            _blackduckApiService = blackduckApiService;
            _excelService = excelService;
            _config = configuration.Get<DART.Core.Config>() ?? throw new ConfigException("Failed to load configuration");
            _logger = logger;
            _blackduckFindingCollector = blackduckFindingCollector ?? new BlackduckFindingCollector();
        }

        /// <summary>
        /// Analyzes Blackduck reports by processing CSV files.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AnalyzeReport()
        {
            var csvFiles = GetCsvFiles();

            if (csvFiles.Length == 0)
            {
                _logger.LogInformation("No CSV files found to process. Exiting analysis.");
                return;
            }

            var latestVersion = await _blackduckApiService.GetLatestProjectVersion(_config.BlackduckConfiguration);
            var configVersions = _config.BlackduckConfiguration.BlackduckRepositories.ToDictionary(r => r.Id, r => r.Versions);
            var collectorOptions = new BlackduckCollectorOptions
            {
                IncludeTransitiveDependency = _config.BlackduckConfiguration.IncludeTransitiveDependency,
                IncludeRecommendedFix = _config.BlackduckConfiguration.IncludeRecommendedFix,
                LatestVersions = latestVersion,
                ConfiguredVersions = configVersions
            };

            foreach (var csvFile in csvFiles)
            {
                var csvData = File.ReadAllLines(csvFile);

                var headers = csvData[0].Split(',');

                if (!FindHeaderIndex(headers))
                {
                    _logger.LogError($"Required columns not found in the {csvFile}. Skipping this file.");
                    continue;
                }

                var rawRows = new List<BlackduckRawFinding>();
                for (int i = 1; i < csvData.Length; i++)
                {
                    var rawFinding = ExtractRawFinding(csvData[i]);
                    if (rawFinding != null)
                    {
                        rawRows.Add(rawFinding);
                    }
                }

                if (rawRows.Count == 0)
                    continue;

                var findings = await _blackduckFindingCollector.CollectAsync(
                    rawRows,
                    collectorOptions,
                    (vulnerabilityId, _) => _blackduckApiService.GetRecommendedFix(_config.BlackduckConfiguration, vulnerabilityId),
                    CancellationToken.None);

                foreach (var finding in findings)
                {
                    _excelService.PopulateRow(new CsvRowDetails
                    {
                        ApplicationName = finding.ApplicationName,
                        SoftwareComponent = finding.SoftwareComponent,
                        Version = finding.Version,
                        SecurityRisk = finding.SecurityRisk,
                        VulnerabilityId = finding.VulnerabilityId,
                        RecommendedFix = finding.RecommendedFix,
                        MatchType = finding.MatchType
                    });
                }
            }
        }

        private string[] GetCsvFiles()
        {
            if (!_config.BlackduckConfiguration.IncludeTransitiveDependency)
            {
                _logger.LogInformation("Skipping all Transitive Dependency as configured in config.json.");
            }

            var reportFolderPath = Path.Combine(_config.ReportConfiguration.OutputFilePath, BlackduckConfiguration.DownloadsFolderName);

            if (!Directory.Exists(reportFolderPath))
            {
                _logger.LogInformation($"Report folder '{reportFolderPath}' does not exist yet. No CSV files to process.");
                return Array.Empty<string>();
            }

            var csvFiles = Directory.GetFiles(reportFolderPath, "*.csv", SearchOption.AllDirectories);

            _logger.LogInformation($"Found {csvFiles.Length} csv file/s in {reportFolderPath}.");

            return csvFiles;
        }

        private bool FindHeaderIndex(string[] headers)
        {
            projectIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ProjectId, StringComparison.OrdinalIgnoreCase));
            projectNameIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ProjectName, StringComparison.OrdinalIgnoreCase));
            componentOriginIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ComponentOriginId, StringComparison.OrdinalIgnoreCase));
            securityRiskIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.SecurityRisk, StringComparison.OrdinalIgnoreCase));
            vulnerabilityIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.VulnerabilityId, StringComparison.OrdinalIgnoreCase));
            matchTypeIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.MatchType, StringComparison.OrdinalIgnoreCase));
            versionIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.Version, StringComparison.OrdinalIgnoreCase));

            return projectIdIndex != -1 && projectNameIndex != -1 && componentOriginIdIndex != -1 && securityRiskIndex != -1 && vulnerabilityIdIndex != -1;
        }

        private BlackduckRawFinding? ExtractRawFinding(string csvRowData)
        {
            var parsedRow = ParseCsvRow(csvRowData);

            if (parsedRow.Length <= Math.Max(versionIndex, Math.Max(matchTypeIndex, vulnerabilityIdIndex)))
            {
                return null;
            }

            return new BlackduckRawFinding(
                parsedRow[projectIdIndex],
                parsedRow[projectNameIndex],
                parsedRow[componentOriginIdIndex],
                parsedRow[securityRiskIndex],
                parsedRow[vulnerabilityIdIndex],
                parsedRow[matchTypeIndex],
                parsedRow[versionIndex]);
        }

        private string[] ParseCsvRow(string row)
        {
            var pattern = "(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)";
            var matches = Regex.Matches(row, pattern);

            var rowData = new List<string>();
            foreach (Match match in matches.Cast<Match>())
            {
                var field = match.Value.Trim('"');
                rowData.Add(field);
            }

            return rowData.ToArray();
        }
    }
}
