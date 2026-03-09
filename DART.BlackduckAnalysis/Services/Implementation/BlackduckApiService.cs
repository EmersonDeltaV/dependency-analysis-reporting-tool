using DART.BlackduckAnalysis.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace DART.BlackduckAnalysis
{
    public class BlackduckApiService : IBlackduckApiService
    {
        private readonly ILogger _logger;
        private string _bearerToken = string.Empty;

        public BlackduckApiService(ILogger<BlackduckApiService> logger)
        {
            _logger = logger;
        }

        private static HttpClient CreateClient(string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        private async Task EnsureBearerTokenConfigured(HttpClient httpClient, BlackduckConfiguration config)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.user-4+json");

            if (string.IsNullOrEmpty(_bearerToken))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"token {config.Token}");

                using var responseToken = await httpClient.PostAsync("api/tokens/authenticate", null);

                if (!responseToken.IsSuccessStatusCode)
                {
                    throw new Exception($"Error getting bearer token. StatusCode={responseToken.StatusCode}");
                }

                var content = await responseToken.Content.ReadAsStringAsync();
                var jsonContent = JObject.Parse(content);
                _bearerToken = jsonContent["bearerToken"]?.ToString() ?? throw new Exception("Error getting bearer token.");
            }

            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
        }

        public async Task<string> GetRecommendedFix(BlackduckConfiguration config, string vulnerabilityId)
        {
            if (string.IsNullOrWhiteSpace(vulnerabilityId))
            {
                return string.Empty;
            }

            string cvePattern = @"CVE-\d{4}-\d{4,7}\s?";
            string cveId = Regex.Replace(vulnerabilityId, cvePattern, "").Trim();
            cveId = Regex.Replace(cveId, @"\(([^)]+)\)", "$1").Trim();

            if (string.IsNullOrWhiteSpace(cveId))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.vulnerability-4+json");

            using var response = await httpClient.GetAsync($"api/vulnerabilities/{cveId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting vulnerability Id {cveId}.");
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var recommendedFix = jsonContent["solution"]?.ToString() ?? string.Empty;

            // Normalize and format the recommended fix text for report output
            var recommendedFixFormatted = RecommendedFixFormatter.Format(recommendedFix);
            return recommendedFixFormatted;
        }

        public async Task<bool> CreateVulnerabilityStatusReport(BlackduckConfiguration config)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            if (config.BlackduckRepositories == null || config.BlackduckRepositories.Count == 0)
            {
                _logger.LogWarning("No Blackduck repositories configured for vulnerability status report.");
                return false;
            }

            _logger.LogInformation($"Creating vulnerability status report for {config.BlackduckRepositories.Count} repositories.");

            var apiPayload = new
            {
                projects = config.BlackduckRepositories?.Select(p => $"{config.BaseUrl}/api/projects/{p.Id}").ToList() ?? new List<string>(),
                reportFormat = "CSV"
            };

            var text = JsonConvert.SerializeObject(apiPayload);
            var httpContent = new StringContent(text, Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync("/api/vulnerability-status-reports", httpContent);

            _logger.LogInformation($"CreateVulnerabilityStatusReport: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetLatestVulnerabilityReportId(BlackduckConfiguration config)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.report-4+json");

            var sortQuery = Uri.EscapeDataString("createdAt desc");
            var requestUri = $"/api/vulnerability-reports?offset=0&limit=100&sort={sortQuery}";

            using var response = await httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var items = jsonContent["items"] as JArray;
            var latestReport = items?
                .OrderByDescending(item => item?["createdAt"])
                .FirstOrDefault();
            var latestReportId = VulnerabilityReportParser.ExtractReportId(latestReport) ?? string.Empty;
            var latestStatus = latestReport?["status"]?.ToString() ?? latestReport?["complete"]?.ToString();

            _logger.LogInformation($"GetLatestVulnerabilityReportId Id: {latestReportId}; Status: {latestStatus ?? "Unknown"}");

            return latestReportId;
        }

        public async Task<bool> GetVulnerabilityStatusReportStatus(BlackduckConfiguration config, string reportId)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.report-4+json");

            var sortQuery = Uri.EscapeDataString("createdAt desc");
            var requestUri = $"/api/vulnerability-reports?offset=0&limit=100&sort={sortQuery}";

            using var response = await httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var items = jsonContent["items"] as JArray;
            var reportEntry = items?
                .Select(item => new { Item = item, Id = VulnerabilityReportParser.ExtractReportId(item) })
                .FirstOrDefault(entry => string.Equals(entry.Id, reportId, StringComparison.OrdinalIgnoreCase));
            var report = reportEntry?.Item;
            var status = report?["status"]?.ToString() ?? report?["complete"]?.ToString();

            _logger.LogInformation($"GetVulnerabilityStatusReportStatus Id: {reportEntry?.Id ?? string.Empty}; Status: {status ?? "Unknown"}");

            return VulnerabilityReportParser.IsReportComplete(report);
        }

        public async Task<string> SaveReport(BlackduckConfiguration config, string reportId, string reportFolderPath)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.report-4+json");

            using var response = await httpClient.GetAsync($"api/reports/{reportId}.json");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return string.Empty;
            }

            var folderPath = reportFolderPath;

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{reportId}.zip");

            using var ms = await response.Content.ReadAsStreamAsync();
            using var fs = File.Create(filePath);
            await ms.CopyToAsync(fs);
            fs.Flush();

            _logger.LogInformation($"File Saved");

            return filePath;

        }

        public async Task<Dictionary<string, string>> GetLatestProjectVersion(BlackduckConfiguration config)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.internal-1+json");

            int boundedCapacity = config.MaxConcurrency;
            var channel = Channel.CreateBounded<BlackduckRepository>(boundedCapacity);
            var projectVersions = new ConcurrentDictionary<string, string>();

            // Producer: write all repos into the channel
            var producer = Task.Run(async () =>
            {
                try
                {
                    foreach (var repo in config.BlackduckRepositories)
                        await channel.Writer.WriteAsync(repo);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });

            // Consumers: up to boundedCapacity parallel HTTP calls
            var consumers = Enumerable.Range(0, boundedCapacity).Select(_ => Task.Run(async () =>
            {
                await foreach (var repo in channel.Reader.ReadAllAsync())
                {
                    var sortQuery = Uri.EscapeDataString("lastScanDate desc");
                    using var response = await httpClient.GetAsync($"api/projects/{repo.Id}/versions?limit=1&offset=0&sort={sortQuery}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Error getting project version for {repo.Name} ({repo.Id}): {errorContent}");
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var jsonContent = JObject.Parse(content);
                    var versions = jsonContent["items"] as JArray;

                    var latestVersion = versions?
                        .OrderByDescending(v => v?["lastScanDate"])
                        .FirstOrDefault()?["versionName"]?.ToString() ?? string.Empty;

                    projectVersions[repo.Id] = latestVersion;

                    _logger.LogInformation($"Latest version for {repo.Name} ({repo.Id}) is {latestVersion}");
                }
            })).ToArray();

            await Task.WhenAll([producer, .. consumers]);

            return new Dictionary<string, string>(projectVersions);
        }
    }
}
