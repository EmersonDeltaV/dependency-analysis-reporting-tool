using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Services;
using Microsoft.Extensions.Logging;

namespace DART.EOLAnalysis
{
    public class EOLAnalysisService : IEOLAnalysisService
    {
        private readonly ILogger<EOLAnalysisService> _logger;
        private readonly INugetMetadataService _nugetMetadata;
        private readonly IAzureDevOpsClientFactory _azureDevOpsClientFactory;
        private readonly IRepositoryProcessorService _repositoryProcessor;
        private readonly IProjectAnalysisService _projectAnalysis;

        public EOLAnalysisService(ILogger<EOLAnalysisService> logger,
                                  INugetMetadataService nugetMetadata,
                                  IAzureDevOpsClientFactory azureDevOpsClientFactory,
                                  IRepositoryProcessorService repositoryProcessor,
                                  IProjectAnalysisService projectAnalysis)
        {
            _logger = logger;
            _nugetMetadata = nugetMetadata;
            _azureDevOpsClientFactory = azureDevOpsClientFactory;
            _repositoryProcessor = repositoryProcessor;
            _projectAnalysis = projectAnalysis;
        }

        public async Task<List<PackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config, CancellationToken cancellationToken = default)
        {
            var results = new List<PackageData>();

            try
            {
                // Initialize NuGet metadata service with configured API URL
                _nugetMetadata.Initialize(config.NuGetApiUrl);

                // Create Azure DevOps client with PAT from config
                using (var azureClient = _azureDevOpsClientFactory.CreateClient(config.Pat))
                {
                    // Process each repository
                    foreach (var repository in config.Repositories)
                    {
                        try
                        {
                            // Create internal Repository instance for processing
                            var internalRepo = new Repository
                            {
                                Name = repository.Name,
                                Url = repository.Url,
                                Branch = repository.Branch
                            };

                            // Step 1: Process repository to get project files
                            var projectInfos = await _repositoryProcessor.ProcessRepositoryAsync(internalRepo, azureClient, cancellationToken);

                            // Step 2: Analyze each project to get package data
                            foreach (var projectInfo in projectInfos)
                            {
                                try
                                {
                                    var packageDataList = await _projectAnalysis.AnalyzeProjectAsync(projectInfo, config.PackageRecommendation, cancellationToken);

                                    // Step 3: Add package data to results
                                    results.AddRange(packageDataList);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to analyze project {ProjectName} in repository {RepositoryName}: {ErrorMessage}",
                                        projectInfo.Name, repository.Name, ex.Message);
                                    // Continue processing other projects
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process repository {RepositoryName}: {ErrorMessage}",
                                repository.Name, ex.Message);
                            // Continue processing other repositories
                        }
                    }

                    _logger.LogInformation("EOL analysis completed. Analyzed {PackageCount} packages", results.Count);
                }
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