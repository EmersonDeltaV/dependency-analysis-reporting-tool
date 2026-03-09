using DART.BlackduckAnalysis;
using DART.Exceptions;
using DART.Models;
using DART.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace DART.Services.Implementation
{
    public class CsvService : ICsvService
    {
        private readonly IBlackduckApiService _blackduckApiService;
        private readonly IExcelService _excelService;
        private readonly ILogger<CsvService> _logger;
        private readonly Config _config;

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
                          ILogger<CsvService> logger)
        {
            _blackduckApiService = blackduckApiService;
            _excelService = excelService;
            _config = configuration.Get<Config>() ?? throw new ConfigException("Failed to load configuration");
            _logger = logger;
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

            foreach (var csvFile in csvFiles)
            {
                var csvData = File.ReadAllLines(csvFile);

                var headers = csvData[0].Split(',');

                if (!FindHeaderIndex(headers))
                {
                    _logger.LogError($"Required columns not found in the {csvFile}. Skipping this file.");
                    continue;
                }

                // Collect all valid rows for this file first
                var validRows = new List<RowDetails>();
                for (int i = 1; i < csvData.Length; i++)
                {
                    var rowDetails = ExtractRowDetails(csvData[i], latestVersion, configVersions);
                    if (rowDetails != null)
                        validRows.Add(rowDetails);
                }

                if (validRows.Count == 0)
                    continue;

                // Fetch recommended fixes in parallel using a bounded channel of size 10
                if (_config.BlackduckConfiguration.IncludeRecommendedFix)
                {
                    var channel = Channel.CreateBounded<RowDetails>(_config.BlackduckConfiguration.BoundedCapacity);

                    var producer = Task.Run(async () =>
                    {
                        try
                        {
                            foreach (var row in validRows)
                                await channel.Writer.WriteAsync(row);
                        }
                        finally
                        {
                            channel.Writer.Complete();
                        }
                    });

                    var consumers = Enumerable.Range(0, _config.BlackduckConfiguration.MaxConcurrency).Select(_ => Task.Run(async () =>
                    {
                        await foreach (var row in channel.Reader.ReadAllAsync())
                        {
                            row.RecommendedFix = await _blackduckApiService.GetRecommendedFix(
                                _config.BlackduckConfiguration, row.VulnerabilityId);
                        }
                    })).ToArray();

                    await Task.WhenAll([producer, .. consumers]);
                }
                else
                {
                    foreach (var row in validRows)
                        row.RecommendedFix = "N/A";
                }

                foreach (var row in validRows)
                    _excelService.PopulateRow(row);
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

        private RowDetails? ExtractRowDetails(string csvRowData, Dictionary<string, string> latestVersions, Dictionary<string, string> configVersions)
        {
            var parsedRow = ParseCsvRow(csvRowData);
            var matchType = parsedRow[matchTypeIndex];
            var version = parsedRow[versionIndex];
            var projectId = parsedRow[projectIdIndex];

            if (!_config.BlackduckConfiguration.IncludeTransitiveDependency &&
                matchType.Equals("Transitive Dependency", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var requestedVersions = configVersions.TryGetValue(projectId, out var versionsString)
                ? versionsString.Split(',').Select(v => v.Trim().ToLowerInvariant()).ToArray()
                : [];
            
            var latestVersion = latestVersions.TryGetValue(projectId, out var latest) ? latest : string.Empty;

            if (!(requestedVersions.Contains(string.Empty) ||
                requestedVersions.Contains(version.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase) ||
                (requestedVersions.Contains("<latest>") && version.Equals(latestVersion, StringComparison.OrdinalIgnoreCase))))
            {
                _logger.LogInformation($"This row with version {version} does not match the required version. Skipping this row.");
                return null;
            }

            var rowDetails = new RowDetails()
            {
                ApplicationName = parsedRow[projectNameIndex],
                SoftwareComponent = parsedRow[componentOriginIdIndex],
                SecurityRisk = parsedRow[securityRiskIndex],
                VulnerabilityId = parsedRow[vulnerabilityIdIndex],
                MatchType = matchType,
                Version = version
            };

            return rowDetails;
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
