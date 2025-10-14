using DART.EOLAnalysis.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DART.EOLAnalysis.Clients
{
    public class AzureDevOpsClient : IAzureDevOpsClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _pat;
        private readonly ILogger<AzureDevOpsClient> _logger;
        private bool _disposed = false;

        public AzureDevOpsClient(string pat, ILogger<AzureDevOpsClient> logger)
        {
            _pat = pat;
            _logger = logger;
            _httpClient = new HttpClient();

            // Setup authentication
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<GitItem>> FindCsProjFilesAsync(Repository repo, CancellationToken cancellationToken = default)
        {
            // API URL to search repository items
            string apiUrl = $"https://dev.azure.com/{repo.Organization}/{repo.Project}/_apis/git/repositories/{repo.RepositoryName}/items?recursionLevel=Full&api-version=7.0";

            if (!string.IsNullOrEmpty(repo.Branch))
            {
                apiUrl += $"&versionDescriptor.version={repo.Branch}&versionDescriptor.versionType=branch";
            }

            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error retrieving .csproj files from repository {RepositoryName}. StatusCode: {StatusCode}",
                    repo.RepositoryName, response.StatusCode);
                return [];
            }

            var content = await response.Content.ReadAsStringAsync();
            var fileList = JsonSerializer.Deserialize<GitItemsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            // Filter for .csproj files
            return fileList?.Value
                .Where(item => item.GitObjectType == "blob"
                            && item.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                            && !item.Path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<GitItem>();
        }

        public async Task<string> GetFileContentAsync(Repository repo, string filePath, CancellationToken cancellationToken = default)
        {
            var downloadUrl = $"https://dev.azure.com/{repo.Organization}/{repo.Project}/_apis/git/repositories/{repo.RepositoryName}/items?path={Uri.EscapeDataString(filePath)}&api-version=7.1&$format=octetStream";

            HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error retrieving file content for {FilePath} from repository {RepositoryName}. StatusCode: {StatusCode}",
                    filePath, repo.RepositoryName, response.StatusCode);
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}