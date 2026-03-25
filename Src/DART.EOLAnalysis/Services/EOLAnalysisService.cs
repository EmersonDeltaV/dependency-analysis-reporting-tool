using Microsoft.Extensions.Logging;

namespace DART.EOLAnalysis
{
    public class EOLAnalysisService : IEOLAnalysisService
    {
        private readonly ILogger<EOLAnalysisService> _logger;
        private readonly INugetMetadataService _nugetMetadata;
        private readonly INpmMetadataService _npmMetadata;
        private readonly IAzureDevOpsClientFactory _azureDevOpsClientFactory;
        private readonly IRepositoryProcessorService _repositoryProcessor;
        private readonly IProjectAnalysisService _projectAnalysis;

        public EOLAnalysisService(ILogger<EOLAnalysisService> logger,
                                  INugetMetadataService nugetMetadata,
                                  INpmMetadataService npmMetadata,
                                  IAzureDevOpsClientFactory azureDevOpsClientFactory,
                                  IRepositoryProcessorService repositoryProcessor,
                                  IProjectAnalysisService projectAnalysis)
        {
            _logger = logger;
            _nugetMetadata = nugetMetadata;
            _npmMetadata = npmMetadata;
            _azureDevOpsClientFactory = azureDevOpsClientFactory;
            _repositoryProcessor = repositoryProcessor;
            _projectAnalysis = projectAnalysis;
        }

        public async Task<List<PackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config, FeatureToggles toggles, CancellationToken cancellationToken = default)
        {
            var results = new List<PackageData>();

            if (!toggles.EnableCSharpAnalysis && !toggles.EnableNpmAnalysis)
            {
                _logger.LogInformation("No ecosystems enabled, skipping EOL analysis.");
                return results;
            }

            try
            {
                if (toggles.EnableCSharpAnalysis)
                    _nugetMetadata.Initialize(config.NuGetApiUrl);

                if (toggles.EnableNpmAnalysis)
                    _npmMetadata.Initialize(config.NpmRegistryUrl);

                using var azureClient = _azureDevOpsClientFactory.CreateClient(config.Pat);

                foreach (var repository in config.Repositories)
                {
                    try
                    {
                        var projectInfos = await _repositoryProcessor.ProcessRepositoryAsync(
                            repository, azureClient, config, toggles, cancellationToken);

                        foreach (var projectInfo in projectInfos)
                        {
                            try
                            {
                                var packages = await _projectAnalysis.AnalyzeProjectAsync(
                                    projectInfo, config, toggles, cancellationToken);
                                results.AddRange(packages);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to analyze project {ProjectName} ({ProjectType}) in repository {RepositoryName}: {ErrorMessage}",
                                    projectInfo.Name, projectInfo.ProjectType, repository.Name, ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process repository {RepositoryName}: {ErrorMessage}",
                            repository.Name, ex.Message);
                    }
                }

                _logger.LogInformation("EOL analysis completed. Analyzed {PackageCount} packages", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during EOL analysis: {ErrorMessage}", ex.Message);
                throw;
            }

            return results;
        }
    }
}
