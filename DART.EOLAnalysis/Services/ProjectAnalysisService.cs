using Microsoft.Extensions.Logging;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Helpers;
using System.Xml.Linq;

namespace DART.EOLAnalysis.Services
{
    public class ProjectAnalysisService : IProjectAnalysisService
    {
        private readonly ILogger<ProjectAnalysisService> _logger;
        private readonly INugetMetadataService _nugetMetadata;
        private readonly IPackageRecommendationService _packageRecommendation;

        public ProjectAnalysisService(ILogger<ProjectAnalysisService> logger,
                                      INugetMetadataService nugetMetadata,
                                      IPackageRecommendationService packageRecommendation)
        {
            _logger = logger;
            _nugetMetadata = nugetMetadata;
            _packageRecommendation = packageRecommendation;
        }

        public async Task<List<PackageData>> AnalyzeProjectAsync(ProjectInfo projectInfo, PackageRecommendationConfig recommendationConfig, CancellationToken cancellationToken = default)
        {
            var packageDataList = new List<PackageData>();

            try
            {
                // Initialize recommendation service with configuration
                _packageRecommendation.Initialize(recommendationConfig);

                _logger.LogInformation("Analyzing project: {ProjectName}", projectInfo.Name);

                // Parse the package references
                IEnumerable<XElement> packageReferences;
                try
                {
                    packageReferences = PackageConfigHelper.GetPackagesFromContent(projectInfo.Content);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse .csproj file {ProjectPath} in project {ProjectName}: {ErrorMessage}",
                        projectInfo.FilePath, projectInfo.Name, ex.Message);
                    return packageDataList; // Return empty list
                }

                if (!packageReferences.Any())
                {
                    _logger.LogInformation("No package references found in project {ProjectName} file {ProjectPath}",
                        projectInfo.Name, projectInfo.FilePath);
                    return packageDataList; // Return empty list
                }

                foreach (var packageReference in packageReferences)
                {
                    var id = packageReference.Attribute("Include")?.Value;
                    var version = packageReference.Attribute("Version")?.Value;

                    if (id != null && version != null)
                    {
                        var packageData = new PackageData()
                        {
                            Id = id,
                            Repository = projectInfo.RepositoryName,
                            Project = projectInfo.Name,
                            Version = version
                        };

                        try
                        {
                            // Get NuGet metadata
                            await _nugetMetadata.GetDataAsync(packageData, cancellationToken);

                            // Determine recommended action based on package data
                            packageData.Action = _packageRecommendation.DetermineAction(packageData);

                            packageDataList.Add(packageData);

                            _logger.LogInformation("Package analyzed: {PackageId} in project {ProjectName}", id, projectInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get NuGet metadata for package {PackageId} in project {ProjectName}: {ErrorMessage}",
                                id, projectInfo.Name, ex.Message);

                            // Add failed package with error info for visibility
                            packageDataList.Add(new PackageData()
                            {
                                Id = id,
                                Repository = projectInfo.RepositoryName,
                                Project = projectInfo.Name,
                                Version = version,
                                Action = $"Error: {ex.Message}"
                            });
                        }
                    }
                    else
                    {
                        _logger.LogWarning("PackageReference element is missing 'Include' or 'Version' attribute in project {ProjectName}",
                            projectInfo.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing project {ProjectName}: {ErrorMessage}", projectInfo.Name, ex.Message);
                throw;
            }

            return packageDataList;
        }
    }
}
