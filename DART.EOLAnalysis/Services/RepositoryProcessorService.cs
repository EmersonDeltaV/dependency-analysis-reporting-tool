using System.Threading.Channels;
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

        public async Task<List<ProjectInfo>> ProcessRepositoryAsync(
            Repository repository,
            IAzureDevOpsClient azureDevOpsClient,
            EOLAnalysisConfig config,
            FeatureToggles toggles,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing repository: {RepositoryName} ({RepositoryUrl})", repository.Name, repository.Url);

                repository.ParseUrl();

                var projectInfos = new List<ProjectInfo>();

                if (toggles.EnableCSharpAnalysis)
                {
                    var csharpProjects = await FindFilesAsync(
                        repository, azureDevOpsClient, config,
                        r => azureDevOpsClient.FindCsProjFilesAsync(r, cancellationToken),
                        ProjectType.CSharp, cancellationToken);

                    if (csharpProjects.Count == 0)
                        _logger.LogWarning("No .csproj files found in repository '{RepositoryName}'.", repository.Name);
                    else
                        _logger.LogInformation("Found {Count} .csproj file(s) in repository {RepositoryName}", csharpProjects.Count, repository.Name);

                    projectInfos.AddRange(csharpProjects);
                }

                if (toggles.EnableNpmAnalysis)
                {
                    var npmProjects = await FindFilesAsync(
                        repository, azureDevOpsClient, config,
                        r => azureDevOpsClient.FindPackageJsonFilesAsync(r, cancellationToken),
                        ProjectType.Npm, cancellationToken);

                    if (npmProjects.Count == 0)
                        _logger.LogWarning("No package.json files found in repository '{RepositoryName}'.", repository.Name);
                    else
                        _logger.LogInformation("Found {Count} package.json file(s) in repository {RepositoryName}", npmProjects.Count, repository.Name);

                    projectInfos.AddRange(npmProjects);
                }

                return projectInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing repository '{RepositoryName}': {ErrorMessage}", repository.Name, ex.Message);
                throw;
            }
        }

        private async Task<List<ProjectInfo>> FindFilesAsync(
            Repository repository,
            IAzureDevOpsClient azureDevOpsClient,
            EOLAnalysisConfig config,
            Func<Repository, Task<List<GitItem>>> findFiles,
            ProjectType projectType,
            CancellationToken cancellationToken)
        {
            var gitItems = await findFiles(repository);

            if (gitItems == null || gitItems.Count == 0)
                return [];

            int boundedCapacity = config.MaxConcurrency;
            var channel = Channel.CreateBounded<GitItem>(boundedCapacity);
            var projectInfos = new System.Collections.Concurrent.ConcurrentBag<ProjectInfo>();

            // Producer: write all git items into the channel
            var producer = Task.Run(async () =>
            {
                try
                {
                    foreach (var gitItem in gitItems)
                        await channel.Writer.WriteAsync(gitItem, cancellationToken);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            // Consumers: spin up bounded-capacity parallel consumers
            var consumers = Enumerable.Range(0, boundedCapacity).Select(_ => Task.Run(async () =>
            {
                await foreach (var gitItem in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    var pathParts = gitItem.Path.Split('/');
                    var projectName = pathParts.Length > 1 ? pathParts[^2] : repository.Name;

                    try
                    {
                        var content = await azureDevOpsClient.GetFileContentAsync(repository, gitItem.Path, cancellationToken);

                        projectInfos.Add(new ProjectInfo
                        {
                            Name = projectName,
                            FilePath = gitItem.Path,
                            Content = content,
                            RepositoryName = repository.RepositoryName,
                            ProjectType = projectType
                        });

                        _logger.LogInformation("Retrieved content for {ProjectType} project: {ProjectName}", projectType, projectName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get content for {FilePath} in repository {RepositoryName}: {ErrorMessage}",
                            gitItem.Path, repository.Name, ex.Message);
                    }
                }
            }, cancellationToken)).ToArray();

            await Task.WhenAll([producer, .. consumers]);

            return [.. projectInfos];
        }
    }
}
