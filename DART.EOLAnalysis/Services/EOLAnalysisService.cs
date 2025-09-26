using Microsoft.Extensions.Logging;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Services;
using DART.EOLAnalysis.Helpers;

namespace DART.EOLAnalysis
{
    public class EOLAnalysisService : IEOLAnalysisService
    {
        private readonly ILogger<EOLAnalysisService> _logger;
        private readonly INugetMetadataService _nugetMetadataService;

        public EOLAnalysisService(ILogger<EOLAnalysisService> logger,
                                  INugetMetadataService nugetMetadataService)
        {
            _logger = logger;
            _nugetMetadataService = nugetMetadataService;
        }

        public async Task<List<EOLPackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config)
        {
            var results = new List<EOLPackageData>();

            try
            {
                // Create Azure DevOps client with PAT from config
                var azureClient = new AzureDevOpsClient(config.Pat);

                // Process each repository
                foreach (var repository in config.Repositories)
                {
                    _logger.LogInformation("Processing repository: {RepositoryName} ({RepositoryUrl})", repository.Name, repository.Url);

                    // Create internal Repository instance and parse URL
                    var internalRepo = new Repository
                    {
                        Name = repository.Name,
                        Url = repository.Url,
                        Branch = repository.Branch
                    };
                    internalRepo.ParseUrl();

                    // Find all .csproj files in the repository
                    var gitItems = await azureClient.FindCsProjFilesAsync(internalRepo);

                    foreach (var gitItem in gitItems)
                    {
                        // Extract project name from path
                        var pathParts = gitItem.Path.Split('/');
                        var projectName = pathParts.Length > 1 ? pathParts[^2] : repository.Name;

                        _logger.LogInformation("Analyzing project: {ProjectName}", projectName);

                        // Get the .csproj file content
                        var csProjContent = await azureClient.GetFileContentAsync(internalRepo, gitItem.Path);

                        // Parse the package references
                        var packageReferences = PackageConfigHelper.GetPackagesFromContent(csProjContent);

                        if (packageReferences is null)
                        {
                            continue;
                        }

                        foreach (var packageReference in packageReferences)
                        {
                            var id = packageReference.Attribute("Include")?.Value;
                            var version = packageReference.Attribute("Version")?.Value;

                            if (id != null && version != null)
                            {
                                var data = new PackageData()
                                {
                                    Id = id,
                                    Project = projectName,
                                    Version = version
                                };

                                // Get NuGet metadata
                                await _nugetMetadataService.GetDataAsync(data);

                                // Convert to our output model
                                var packageData = new EOLPackageData
                                {
                                    Id = data.Id,
                                    Project = data.Project,
                                    Version = data.Version,
                                    VersionDate = data.VersionDate ?? string.Empty,
                                    Age = data.Age,
                                    LatestVersion = data.LatestVersion ?? string.Empty,
                                    LatestVersionDate = data.LatestVersionDate ?? string.Empty,
                                    License = data.License ?? string.Empty,
                                    LicenseUrl = data.LicenseUrl ?? string.Empty,
                                    Action = data.Action ?? string.Empty
                                };

                                results.Add(packageData);

                                _logger.LogInformation("Package analyzed: {PackageId}", id);
                            }
                            else
                            {
                                _logger.LogWarning("PackageReference element is missing 'Include' or 'Version' attribute in project {ProjectName}", projectName);
                            }
                        }
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