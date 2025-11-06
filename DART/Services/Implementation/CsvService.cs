using DART.BlackduckAnalysis;
using DART.Exceptions;
using DART.Models;
using DART.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DART.Services.Implementation
{
    public class CsvService : ICsvService
    {
        private readonly IBlackduckApiService _blackduckApiService;
        private readonly IExcelService _excelService;
        private readonly ILogger<CsvService> _logger;
        private readonly Config _config;

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

            foreach (var csvFile in csvFiles)
            {
                var csvData = File.ReadAllLines(csvFile);

                var headers = csvData[0].Split(',');

                if (!FindHeaderIndex(headers))
                {
                    _logger.LogError($"Required columns not found in the {csvFile}. Skipping this file.");
                    continue;
                }

                for (int i = 1; i < csvData.Length; i++)
                {
                    var rowDetails = ExtractRowDetails(csvData[i]);

                    if (rowDetails == null)
                    {
                        continue;
                    }

                    var recommendedFix = await _blackduckApiService.GetRecommendedFix(_config.BlackduckConfiguration, rowDetails.VulnerabilityId);

                    rowDetails.RecommendedFix = recommendedFix;

                    _excelService.PopulateRow(rowDetails);
                }
            }
        }

        private string[] GetCsvFiles()
        {
            if (!_config.BlackduckConfiguration.IncludeTransitiveDependency)
            {
                _logger.LogInformation("Skipping all Transitive Dependency as configured in config.json.");
            }

            var reportFolderPath = _config.ReportConfiguration.ReportFolderPath;

            var csvFiles = Directory.GetFiles(reportFolderPath, "*.csv", SearchOption.AllDirectories);

            _logger.LogInformation($"Found {csvFiles.Length} csv file/s in {reportFolderPath}.");

            return csvFiles;
        }

        private bool FindHeaderIndex(string[] headers)
        {
            projectNameIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ProjectName, StringComparison.OrdinalIgnoreCase));
            componentOriginIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ComponentOriginId, StringComparison.OrdinalIgnoreCase));
            securityRiskIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.SecurityRisk, StringComparison.OrdinalIgnoreCase));
            vulnerabilityIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.VulnerabilityId, StringComparison.OrdinalIgnoreCase));
            matchTypeIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.MatchType, StringComparison.OrdinalIgnoreCase));
            versionIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.Version, StringComparison.OrdinalIgnoreCase));

            return projectNameIndex != -1 && componentOriginIdIndex != -1 && securityRiskIndex != -1 && vulnerabilityIdIndex != -1;
        }

        private RowDetails? ExtractRowDetails(string csvRowData)
        {
            var parsedRow = ParseCsvRow(csvRowData);
            var matchType = parsedRow[matchTypeIndex];
            var version = parsedRow[versionIndex];

            var versions = _config.BlackduckConfiguration.ProjectVersionsToInclude.Split(',');

            if (!_config.BlackduckConfiguration.IncludeTransitiveDependency &&
                matchType.Equals("Transitive Dependency", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!versions.Contains(string.Empty) && !versions.Contains(version))
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
