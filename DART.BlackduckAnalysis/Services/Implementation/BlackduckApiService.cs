using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

            var apiPayload = new
            {
                projects = config.BlackduckRepositories?.Select(p => p.Url).ToList() ?? new List<string>(),
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

            using var response = await httpClient.GetAsync("/api/v1/vulnerability-reports?ascending=false&limit=100&offset=0&sortField=finishedAt");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var latestReport = jsonContent["items"]?.OrderByDescending(item => item["createdAt"])?.FirstOrDefault();
            var latestReportId = latestReport?["id"]?.ToString() ?? string.Empty;

            _logger.LogInformation($"GetLatestVulnerabilityReportId Id: {latestReport?["id"]}; " +
                $"Complete Status: {latestReport?["complete"]?.ToString()}");

            return latestReportId;
        }

        public async Task<bool> GetVulnerabilityStatusReportCompleteStatus(BlackduckConfiguration config, string reportId)
        {
            if (string.IsNullOrEmpty(config.BaseUrl))
            {
                throw new InvalidOperationException("BlackduckConfiguration:BaseUrl is not configured. Please check your config.json file.");
            }

            using var httpClient = CreateClient(config.BaseUrl);
            await EnsureBearerTokenConfigured(httpClient, config);

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.report-4+json");

            using var response = await httpClient.GetAsync("/api/v1/vulnerability-reports?ascending=false&limit=100&offset=0&sortField=finishedAt");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return false;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var report = jsonContent["items"]?.Where(item => item["id"].ToString() == reportId)?.FirstOrDefault();

            _logger.LogInformation($"GetVulnerabilityStatusReportCompleteStatus Id: {report?["id"]}; " +
                $"Complete Status: {report?["complete"]?.ToString()}");

            return bool.Parse(report?["complete"]?.ToString() ?? bool.FalseString);
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

            using var response = await httpClient.GetAsync($"api/v1/reports/{reportId}.json");
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

    }
}
