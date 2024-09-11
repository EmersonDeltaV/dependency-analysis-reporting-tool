using BlackduckReportAnalysis.Models;
using System.Text.RegularExpressions;

namespace BlackduckReportAnalysis
{
    public static class CsvService
    {
        private static int projectNameIndex;
        private static int componentOriginIdIndex;
        private static int securityRiskIndex;
        private static int vulnerabilityIdIndex;
        private static int matchTypeIndex;

        /// <summary>
        /// Analyzes Blackduck reports by processing CSV files.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task AnalyzeReport()
        {
            var csvFiles = GetCsvFiles();

            foreach (var csvFile in csvFiles)
            {
                var csvData = File.ReadAllLines(csvFile);

                var headers = csvData[0].Split(',');

                if (!FindHeaderIndex(headers))
                {
                    SeriLogger.Error($"Required columns not found in the {csvFile}. Skipping this file.");
                    continue;
                }

                for (int i = 1; i < csvData.Length; i++)
                {
                    var rowDetails = ExtractRowDetails(csvData[i]);

                    if (rowDetails == null)
                    {
                        continue;
                    }

                    var recommendedFix = await BlackduckApiService.GetRecommendedFix(rowDetails.VulnerabilityId);

                    rowDetails.RecommendedFix = recommendedFix;

                    ExcelService.PopulateRow(rowDetails);
                }
            }
        }

        private static string[] GetCsvFiles()
        {
            if (!ConfigService.Config.IncludeTransitiveDependency)
            {
                SeriLogger.Information("Skipping all Transitive Dependency as configured in config.json.");
            }

            var reportFolderPath = ConfigService.Config.ReportFolderPath;

            var csvFiles = Directory.GetFiles(reportFolderPath, "*.csv", SearchOption.AllDirectories);

            SeriLogger.Information($"Found {csvFiles.Length} csv file/s in {reportFolderPath}.");

            return csvFiles;
        }

        private static bool FindHeaderIndex(string[] headers)
        {
            projectNameIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ProjectName, StringComparison.OrdinalIgnoreCase));
            componentOriginIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.ComponentOriginId, StringComparison.OrdinalIgnoreCase));
            securityRiskIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.SecurityRisk, StringComparison.OrdinalIgnoreCase));
            vulnerabilityIdIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.VulnerabilityId, StringComparison.OrdinalIgnoreCase));
            matchTypeIndex = Array.FindIndex(headers, x => x.Equals(BlackduckCSVHeaders.MatchType, StringComparison.OrdinalIgnoreCase));

            return projectNameIndex != -1 && componentOriginIdIndex != -1 && securityRiskIndex != -1 && vulnerabilityIdIndex != -1;
        }

        private static RowDetails? ExtractRowDetails(string csvRowData)
        {
            var parsedRow = ParseCsvRow(csvRowData);

            var matchType = parsedRow[matchTypeIndex];

            if (!ConfigService.Config.IncludeTransitiveDependency &&
                matchType.Equals("Transitive Dependency", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rowDetails = new RowDetails()
            {
                ApplicationName = parsedRow[projectNameIndex],
                SoftwareComponent = parsedRow[componentOriginIdIndex],
                SecurityRisk = parsedRow[securityRiskIndex],
                VulnerabilityId = parsedRow[vulnerabilityIdIndex],
                MatchType = matchType
            };

            return rowDetails;
        }

        private static string[] ParseCsvRow(string row)
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
