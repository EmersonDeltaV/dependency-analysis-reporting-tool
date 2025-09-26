using Microsoft.Extensions.Logging;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Clients;

namespace DART.EOLAnalysis.Services
{
    public class RepositoryProcessorService : IRepositoryProcessorService
    {
        private readonly ILogger<RepositoryProcessorService> _logger;

        public RepositoryProcessorService(ILogger<RepositoryProcessorService> logger)
        {
            _logger = logger;
        }

        public async Task<List<ProjectInfo>> ProcessRepositoryAsync(Repository repository, IAzureDevOpsClient azureDevOpsClient, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing repository: {RepositoryName} ({RepositoryUrl})", repository.Name, repository.Url);

                // Parse repository URL to get organization, project, and repo name
                repository.ParseUrl();

                var projectInfos = await FindProjectFilesAsync(repository, azureDevOpsClient, cancellationToken);

                _logger.LogInformation("Found {ProjectCount} .csproj files in repository {RepositoryName}",
                    projectInfos.Count, repository.Name);

                return projectInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing repository {RepositoryName}: {ErrorMessage}",
                    repository.Name, ex.Message);
                throw;
            }
        }

        private async Task<List<ProjectInfo>> FindProjectFilesAsync(Repository repository, IAzureDevOpsClient azureDevOpsClient, CancellationToken cancellationToken)
        {
            var projectInfos = new List<ProjectInfo>();

            // Find all .csproj files in the repository
            var gitItems = await azureDevOpsClient.FindCsProjFilesAsync(repository, cancellationToken);

            foreach (var gitItem in gitItems)
            {
                // Extract project name from path
                var pathParts = gitItem.Path.Split('/');
                var projectName = pathParts.Length > 1 ? pathParts[^2] : repository.Name;

                try
                {
                    // Get the .csproj file content
                    var csProjContent = await azureDevOpsClient.GetFileContentAsync(repository, gitItem.Path, cancellationToken);

                    projectInfos.Add(new ProjectInfo
                    {
                        Name = projectName,
                        FilePath = gitItem.Path,
                        Content = csProjContent
                    });

                    _logger.LogInformation("Retrieved content for project: {ProjectName}", projectName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get content for project file {ProjectPath} in project {ProjectName}: {ErrorMessage}",
                        gitItem.Path, projectName, ex.Message);
                    // Continue processing other files
                }
            }

            return projectInfos;
        }
    }
}