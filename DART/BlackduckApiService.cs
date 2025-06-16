using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BlackduckReportAnalysis
{

    /// <summary>
    /// Provides methods to interact with the Blackduck API.
    /// </summary>
    public static class BlackduckApiService
    {
        private static readonly HttpClientHandler httpClientHandler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
        };

        private static readonly HttpClient httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(ConfigService.Config.BaseUrl)
        };

        private static string BearerToken = string.Empty;

        /// <summary>
        /// Retrieves the recommended fix for a given vulnerability ID.
        /// </summary>
        /// <param name="vulnerabilityId">The vulnerability ID.</param>
        /// <returns>The recommended fix for the vulnerability.</returns>
        public static async Task<string> GetRecommendedFix(string vulnerabilityId)
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

            await GetBearerToken();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.vulnerability-4+json");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {BearerToken}");

            var response = await httpClient.GetAsync($"api/vulnerabilities/{cveId}");

            if (!response.IsSuccessStatusCode)
            {
                SeriLogger.Error($"Error getting vulnerability Id {cveId}.");
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

        private static async Task GetBearerToken()
        {
            if (!string.IsNullOrEmpty(BearerToken)) return;

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.blackducksoftware.user-4+json");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {ConfigService.Config.Token}");

            var responseToken = await httpClient.PostAsync("api/tokens/authenticate", null);

            if (!responseToken.IsSuccessStatusCode)
            {
                throw new Exception($"Error getting bearer token. StatusCode={responseToken.StatusCode}");
            }

            var content = await responseToken.Content.ReadAsStringAsync();
            var jsonContent = JObject.Parse(content);
            BearerToken = jsonContent["bearerToken"]?.ToString() ?? throw new Exception("Error getting bearer token.");
        }
    }
}
