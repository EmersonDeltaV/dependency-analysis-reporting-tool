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
            var fileItems = await GetRepositoryItemsAsync(repo, ".csproj files", cancellationToken);
            var filters = repo.FileSkipFilter.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter for .csproj files
            return fileItems
                .Where(item => item.GitObjectType == "blob"
                            && item.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                            && !IsPathExcludedForCsProjLikeDiscovery(item.Path, filters))
                .ToList();
        }

        public async Task<List<GitItem>> FindPackageJsonFilesAsync(Repository repo, CancellationToken cancellationToken = default)
        {
            var fileItems = await GetRepositoryItemsAsync(repo, "package.json files", cancellationToken);
            var filters = repo.FileSkipFilter.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter for package.json files, excluding node_modules and any user-defined skip paths
            return fileItems
                .Where(item => item.GitObjectType == "blob"
                            && item.Path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)
                            && !item.Path.Contains("node_modules", StringComparison.OrdinalIgnoreCase)
                            && !IsPathExcludedByFilters(item.Path, filters))
                .ToList();
        }

        public async Task<List<GitItem>> FindDirectoryPackagesPropsFilesAsync(Repository repo, CancellationToken cancellationToken = default)
        {
            var fileItems = await GetRepositoryItemsAsync(repo, "Directory.Packages.props files", cancellationToken);
            var filters = repo.FileSkipFilter.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return fileItems
                .Where(item => item.GitObjectType == "blob"
                            && IsDirectoryPackagesPropsFile(item.Path)
                            && !IsPathExcludedForCsProjLikeDiscovery(item.Path, filters))
                .ToList();
        }

        public async Task<string> GetFileContentAsync(Repository repo, string filePath, CancellationToken cancellationToken = default)
        {
            var downloadUrl = $"https://dev.azure.com/{repo.Organization}/{repo.Project}/_apis/git/repositories/{repo.RepositoryName}/items?path={Uri.EscapeDataString(filePath)}&api-version=7.1&$format=octetStream";
            downloadUrl = AppendBranchQuery(downloadUrl, repo.Branch);

            using HttpResponseMessage response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to retrieve file content for '{FilePath}' from repository '{RepositoryName}'. Status: {StatusCode} ({ReasonPhrase}). Response: {ErrorContent}",
                    filePath, repo.RepositoryName, (int)response.StatusCode, response.ReasonPhrase, errorContent);
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
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

        private async Task<List<GitItem>> GetRepositoryItemsAsync(Repository repo, string lookupDescription, CancellationToken cancellationToken)
        {
            var apiUrl = AppendBranchQuery(
                $"https://dev.azure.com/{repo.Organization}/{repo.Project}/_apis/git/repositories/{repo.RepositoryName}/items?recursionLevel=Full&api-version=7.0",
                repo.Branch);

            using HttpResponseMessage response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NonAuthoritativeInformation)
            {
                _logger.LogError("Failed to retrieve {LookupDescription} from repository '{RepositoryName}'. Status: {StatusCode} ({ReasonPhrase})",
                    lookupDescription, repo.RepositoryName, (int)response.StatusCode, response.ReasonPhrase);
                return [];
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var fileList = JsonSerializer.Deserialize<GitItemsResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                return fileList?.Value ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to parse JSON response from Azure DevOps API for repository '{RepositoryName}' while retrieving {LookupDescription}. Error: {ErrorMessage}",
                    repo.RepositoryName, lookupDescription, ex.Message);
                return [];
            }
        }

        private static string AppendBranchQuery(string url, string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
            {
                return url;
            }

            return $"{url}&versionDescriptor.version={Uri.EscapeDataString(branch)}&versionDescriptor.versionType=branch";
        }

        private static bool IsPathExcludedForCsProjLikeDiscovery(string path, HashSet<string> filters)
        {
            return path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)
                || IsPathExcludedByFilters(path, filters);
        }

        private static bool IsPathExcludedByFilters(string path, HashSet<string> filters)
        {
            return filters.Any(filter => path.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsDirectoryPackagesPropsFile(string path)
        {
            var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return pathSegments.Length > 0
                && string.Equals(pathSegments[^1], "Directory.Packages.props", StringComparison.OrdinalIgnoreCase);
        }
    }
}