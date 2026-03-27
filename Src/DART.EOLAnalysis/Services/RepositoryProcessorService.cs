using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DART.EOLAnalysis
{
    public class RepositoryProcessorService : IRepositoryProcessorService
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyDirectoryPackagesPropsContext =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger<RepositoryProcessorService> _logger;

        public RepositoryProcessorService(ILogger<RepositoryProcessorService> logger)
        {
            _logger = logger;
        }

        public async Task<List<ProjectInfo>> ProcessRepositoryAsync(
            Repository repository,
            IAzureDevOpsClient azureDevOpsClient,
            EOLAnalysisConfig config,
            EolFeatureToggles toggles,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing repository: {RepositoryName} ({RepositoryUrl})", repository.Name, repository.Url);

                repository.ParseUrl();

                var projectInfos = new List<ProjectInfo>();

                IReadOnlyDictionary<string, string> directoryPackagesPropsByPath = EmptyDirectoryPackagesPropsContext;

                if (toggles.EnableCSharpAnalysis)
                {
                    directoryPackagesPropsByPath = await LoadDirectoryPackagesPropsContextAsync(repository, azureDevOpsClient, cancellationToken);

                    if (directoryPackagesPropsByPath.Count == 0)
                        _logger.LogInformation("No Directory.Packages.props files found in repository '{RepositoryName}'.", repository.Name);
                    else
                        _logger.LogInformation("Found {Count} Directory.Packages.props file(s) in repository {RepositoryName}", directoryPackagesPropsByPath.Count, repository.Name);

                    var csharpProjects = await FindFilesAsync(
                        repository, azureDevOpsClient, config,
                        r => azureDevOpsClient.FindCsProjFilesAsync(r, cancellationToken),
                        ProjectType.CSharp,
                        directoryPackagesPropsByPath,
                        cancellationToken);

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
                        ProjectType.Npm,
                        EmptyDirectoryPackagesPropsContext,
                        cancellationToken);

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
            IReadOnlyDictionary<string, string> directoryPackagesPropsByPath,
            CancellationToken cancellationToken)
        {
            var gitItems = await findFiles(repository);

            if (gitItems == null || gitItems.Count == 0)
                return [];

            var channel = Channel.CreateBounded<GitItem>(config.BoundedCapacity);
            var projectInfos = new ConcurrentBag<ProjectInfo>();

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

            // Consumers: spin up config.MaxConcurrency parallel consumers
            var consumers = Enumerable.Range(0, config.MaxConcurrency).Select(_ => Task.Run(async () =>
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
                            ProjectType = projectType,
                            DirectoryPackagesPropsByPath = projectType == ProjectType.CSharp
                                ? directoryPackagesPropsByPath
                                : EmptyDirectoryPackagesPropsContext
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

        private async Task<IReadOnlyDictionary<string, string>> LoadDirectoryPackagesPropsContextAsync(
            Repository repository,
            IAzureDevOpsClient azureDevOpsClient,
            CancellationToken cancellationToken)
        {
            var propsFiles = await azureDevOpsClient.FindDirectoryPackagesPropsFilesAsync(repository, cancellationToken);

            if (propsFiles == null || propsFiles.Count == 0)
            {
                return EmptyDirectoryPackagesPropsContext;
            }

            var directoryPackagesPropsByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var propsFile in propsFiles)
            {
                try
                {
                    var content = await azureDevOpsClient.GetFileContentAsync(repository, propsFile.Path, cancellationToken);

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Directory.Packages.props file '{FilePath}' in repository '{RepositoryName}' returned empty content and will be skipped.",
                            propsFile.Path, repository.Name);
                        continue;
                    }

                    directoryPackagesPropsByPath[propsFile.Path] = content;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get content for Directory.Packages.props file {FilePath} in repository {RepositoryName}: {ErrorMessage}",
                        propsFile.Path, repository.Name, ex.Message);
                }
            }

            return directoryPackagesPropsByPath.Count == 0
                ? EmptyDirectoryPackagesPropsContext
                : directoryPackagesPropsByPath;
        }
    }
}
