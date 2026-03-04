using System.Threading.Channels;
using System.Collections.Concurrent;
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
        private readonly INpmMetadataService _npmMetadata;
        private readonly IPackageRecommendationService _packageRecommendation;

        public ProjectAnalysisService(ILogger<ProjectAnalysisService> logger,
                                      INugetMetadataService nugetMetadata,
                                      INpmMetadataService npmMetadata,
                                      IPackageRecommendationService packageRecommendation)
        {
            _logger = logger;
            _nugetMetadata = nugetMetadata;
            _npmMetadata = npmMetadata;
            _packageRecommendation = packageRecommendation;
        }

        public Task<List<PackageData>> AnalyzeProjectAsync(
            ProjectInfo projectInfo,
            PackageRecommendationConfig recommendationConfig,
            FeatureToggles toggles,
            CancellationToken cancellationToken = default)
        {
            return projectInfo.ProjectType switch
            {
                ProjectType.Npm => AnalyzeNpmProjectAsync(projectInfo, recommendationConfig, toggles.IncludeNpmDevDependencies, cancellationToken),
                _ => AnalyzeCSharpProjectAsync(projectInfo, recommendationConfig, cancellationToken),
            };
        }

        private async Task<List<PackageData>> AnalyzeCSharpProjectAsync(
            ProjectInfo projectInfo,
            PackageRecommendationConfig recommendationConfig,
            CancellationToken cancellationToken)
        {
            try
            {
                _packageRecommendation.Initialize(recommendationConfig);

                _logger.LogInformation("Analyzing C# project: {ProjectName}", projectInfo.Name);

                IEnumerable<XElement> packageReferences;
                try
                {
                    packageReferences = PackageConfigHelper.GetPackagesFromContent(projectInfo.Content);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse .csproj file {ProjectPath} in project {ProjectName}: {ErrorMessage}",
                        projectInfo.FilePath, projectInfo.Name, ex.Message);
                    return [];
                }

                var packageList = packageReferences
                    .Select(r => (Id: r.Attribute("Include")?.Value, Version: r.Attribute("Version")?.Value))
                    .Where(p =>
                    {
                        if (p.Id == null || p.Version == null)
                        {
                            _logger.LogWarning("PackageReference element is missing 'Include' or 'Version' attribute in project {ProjectName}", projectInfo.Name);
                            return false;
                        }
                        return true;
                    })
                    .Select(p => (p.Id!, p.Version!))
                    .ToList();

                if (packageList.Count == 0)
                {
                    _logger.LogInformation("No package references found in project {ProjectName} file {ProjectPath}",
                        projectInfo.Name, projectInfo.FilePath);
                    return [];
                }

                var skipPatterns = NormalizeSkipPatterns(recommendationConfig);

                return await ProcessPackagesInParallelAsync(
                    packageList,
                    projectInfo,
                    recommendationConfig,
                    skipPatterns,
                    async (packageData, ct) =>
                    {
                        await _nugetMetadata.GetDataAsync(packageData, ct);
                        packageData.Action = _packageRecommendation.DetermineAction(packageData);
                        _logger.LogInformation("Package analyzed: {PackageId} in project {ProjectName}", packageData.Id, projectInfo.Name);
                    },
                    "NuGet",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing C# project {ProjectName}: {ErrorMessage}", projectInfo.Name, ex.Message);
                throw;
            }
        }

        private async Task<List<PackageData>> AnalyzeNpmProjectAsync(
            ProjectInfo projectInfo,
            PackageRecommendationConfig recommendationConfig,
            bool includeDevDependencies,
            CancellationToken cancellationToken)
        {
            try
            {
                _packageRecommendation.Initialize(recommendationConfig);

                _logger.LogInformation("Analyzing npm project: {ProjectName} (IncludeDevDependencies: {IncludeDevDependencies})", projectInfo.Name, includeDevDependencies);

                IEnumerable<(string Name, string Version)> packageReferences;
                try
                {
                    packageReferences = PackageJsonHelper.GetPackagesFromContent(projectInfo.Content, includeDevDependencies);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse package.json file {ProjectPath} in project {ProjectName}: {ErrorMessage}",
                        projectInfo.FilePath, projectInfo.Name, ex.Message);
                    return [];
                }

                var packageList = packageReferences.ToList();

                if (packageList.Count == 0)
                {
                    _logger.LogInformation("No package references found in project {ProjectName} file {ProjectPath}",
                        projectInfo.Name, projectInfo.FilePath);
                    return [];
                }

                var skipPatterns = NormalizeSkipPatterns(recommendationConfig);

                return await ProcessPackagesInParallelAsync(
                    packageList,
                    projectInfo,
                    recommendationConfig,
                    skipPatterns,
                    async (packageData, ct) =>
                    {
                        await _npmMetadata.GetDataAsync(packageData, ct);
                        packageData.Action = _packageRecommendation.DetermineAction(packageData);
                        _logger.LogInformation("npm package analyzed: {PackageId} in project {ProjectName}", packageData.Id, projectInfo.Name);
                    },
                    "npm",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing npm project {ProjectName}: {ErrorMessage}", projectInfo.Name, ex.Message);
                throw;
            }
        }

        private async Task<List<PackageData>> ProcessPackagesInParallelAsync(
            List<(string Id, string Version)> packageList,
            ProjectInfo projectInfo,
            PackageRecommendationConfig recommendationConfig,
            List<string> skipPatterns,
            Func<PackageData, CancellationToken, Task> fetchMetadata,
            string metadataSource,
            CancellationToken cancellationToken)
        {
            const int boundedCapacity = 10;
            var channel = Channel.CreateBounded<(string Id, string Version)>(boundedCapacity);
            var results = new ConcurrentBag<PackageData>();

            // Producer: enqueue all packages; skipped ones are handled inline before writing
            var producer = Task.Run(async () =>
            {
                try
                {
                    foreach (var package in packageList)
                    {
                        if (ShouldSkip(package.Id, skipPatterns))
                        {
                            results.Add(new PackageData
                            {
                                Id = package.Id,
                                Repository = projectInfo.RepositoryName,
                                Project = projectInfo.Name,
                                Version = package.Version,
                                Action = recommendationConfig!.Messages.SkipInternal
                            });
                            _logger.LogInformation("Package {PackageId} in project {ProjectName} marked as '{Action}' due to SkipInternalPackagesFilter",
                                package.Id, projectInfo.Name, recommendationConfig!.Messages.SkipInternal);
                        }
                        else
                        {
                            await channel.Writer.WriteAsync(package, cancellationToken);
                        }
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            // Consumers: up to boundedCapacity parallel metadata fetches
            var consumers = Enumerable.Range(0, boundedCapacity).Select(_ => Task.Run(async () =>
            {
                await foreach (var (id, version) in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    var packageData = new PackageData
                    {
                        Id = id,
                        Repository = projectInfo.RepositoryName,
                        Project = projectInfo.Name,
                        Version = version
                    };

                    try
                    {
                        await fetchMetadata(packageData, cancellationToken);
                        results.Add(packageData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get {MetadataSource} metadata for package {PackageId} in project {ProjectName}: {ErrorMessage}",
                            metadataSource, id, projectInfo.Name, ex.Message);
                        results.Add(new PackageData
                        {
                            Id = id,
                            Repository = projectInfo.RepositoryName,
                            Project = projectInfo.Name,
                            Version = version,
                            Action = $"Error: {ex.Message}"
                        });
                    }
                }
            }, cancellationToken)).ToArray();

            await Task.WhenAll([producer, .. consumers]);

            return [.. results];
        }

        private static List<string> NormalizeSkipPatterns(PackageRecommendationConfig recommendationConfig)
            => (recommendationConfig?.SkipInternalPackagesFilter ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();

        private static bool ShouldSkip(string packageId, IReadOnlyCollection<string> skipPatterns)
        {
            if (skipPatterns == null || skipPatterns.Count == 0)
                return false;

            foreach (var pattern in skipPatterns)
            {
                if (IsWildcardMatch(packageId, pattern))
                    return true;
            }
            return false;
        }

        private static bool IsWildcardMatch(string input, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            string regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            regexPattern = "^" + regexPattern + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
