using DART.BlackduckAnalysis.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DART.BlackduckAnalysis.Integration.Implementation
{
    public class BlackduckApiService : IBlackduckApiService
    {
        private const string KEY_VULNERABILITY_REPORT_PARAMETERS = "DownloadConfiguration:VulnerabilityReportParameters";
        private const string KEY_BASEURL = "BaseUrl";
        private const string KEY_TOKEN = "BlackduckToken";
        private const string KEY_REPORT_FOLDER_PATH = "ReportFolderPath";
        private const string DL_FOLDER_PATH = "Downloaded";

        private readonly IConfiguration configuration;
        private readonly ILogger _logger;
        private readonly HttpClientHandler httpClientHandler;
        private readonly HttpClient httpClient;

        private string BearerToken = string.Empty;

        public BlackduckApiService(ILogger<BlackduckApiService> logger, IConfiguration configuration)
        {
            this.configuration = configuration;

            _logger = logger;

            httpClientHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
            };
            httpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = new Uri(configuration.GetSection(KEY_BASEURL).Value?? string.Empty)
            };
        }

        private async Task EnsureBearerTokenConfigured()
        {
            if (!string.IsNullOrEmpty(BearerToken)) return;

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.user-4+json");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {configuration.GetSection(KEY_TOKEN).Value}");

            var responseToken = await httpClient.PostAsync("api/tokens/authenticate", null);

            if (!responseToken.IsSuccessStatusCode)
            {
                throw new Exception($"Error getting bearer token. StatusCode={responseToken.StatusCode}");
            }

            var content = await responseToken.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            BearerToken = jsonContent["bearerToken"]?.ToString() ?? throw new Exception("Error getting bearer token.");

            httpClient.DefaultRequestHeaders.Remove("Authorization");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {BearerToken}");
        }

        public async Task<string> GetRecommendedFix(string vulnerabilityId)
        {
            if (string.IsNullOrWhiteSpace(vulnerabilityId))
            {
                return string.Empty;
            }

            string cvePattern = @"CVE-\d{4}-\d{4,7}\s?"; // Matches CVE- followed by 4
            string cveId = Regex.Replace(vulnerabilityId, cvePattern, "").Trim();
            cveId = Regex.Replace(cveId, @"\(([^)]+)\)", "$1").Trim();

            if (string.IsNullOrWhiteSpace(cveId))
            {
                return string.Empty;
            }

            await EnsureBearerTokenConfigured();

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.vulnerability-4+json");

            var response = await httpClient.GetAsync($"api/vulnerabilities/{cveId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting vulnerability Id {cveId}.");
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            var recommendedFix = jsonContent["solution"]?.ToString() ?? string.Empty;

            // Removing all asterisks and texts inside parentheses
            recommendedFix = Regex.Replace(recommendedFix, @"[\*\[\]\n]", "").Trim();
            recommendedFix = Regex.Replace(recommendedFix, @"\([^)]+\)", "").Trim();

            return recommendedFix;
        }

        public async Task<bool> CreateVulnerabilityStatusReport()
        {
            await EnsureBearerTokenConfigured();

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var vulnerabilityReportParameters = configuration
                .GetSection(KEY_VULNERABILITY_REPORT_PARAMETERS)
                .Get<VulnerabilityReportParameters>();

            var text = JsonConvert.SerializeObject(vulnerabilityReportParameters);

            var httpContent = new StringContent(text, Encoding.UTF8, "application/json");

            using var response = await httpClient.PostAsync("/api/vulnerability-status-reports", httpContent);

            _logger.LogInformation($"CreateVulnerabilityStatusReport: {response.StatusCode}");

            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetLatestVulnerabilityReportId()
        {
            await EnsureBearerTokenConfigured();

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

        public async Task<bool> GetVulnerabilityStatusReportCompleteStatus(string reportId)
        {
            await EnsureBearerTokenConfigured();

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

        public async Task<string> SaveReport(string reportId)
        {
            await EnsureBearerTokenConfigured();

            httpClient.DefaultRequestHeaders.Remove("Accept");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.report-4+json");

            using var response = await httpClient.GetAsync($"api/v1/reports/{reportId}.json");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error getting report.");
                return string.Empty;
            }

            var folderPath = Path.Combine(configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value, DL_FOLDER_PATH);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Path to save the file
            var filePath = Path.Combine(folderPath, $"{reportId}.zip");

            // Read the content into a MemoryStream and then write to file
            using var ms = await response.Content.ReadAsStreamAsync();
            using var fs = File.Create(filePath);
            await ms.CopyToAsync(fs);
            fs.Flush();


            _logger.LogInformation($"File Saved");

            return filePath;

        }

    }
}