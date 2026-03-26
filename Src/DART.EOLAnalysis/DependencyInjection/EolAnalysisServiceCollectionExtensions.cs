using DART.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DART.EOLAnalysis;

public static class EolAnalysisServiceCollectionExtensions
{
    public static IServiceCollection AddEolAnalysis(this IServiceCollection services)
    {
        services.TryAddSingleton<IEolAnalyzer, EolAnalyzerAdapter>();
        services.TryAddSingleton<IEOLAnalysisService, EOLAnalysisService>();
        services.TryAddSingleton<INugetMetadataService, NugetMetadataService>();
        services.TryAddSingleton<INpmMetadataService, NpmMetadataService>();
        services.TryAddSingleton<IAzureDevOpsClientFactory, AzureDevOpsClientFactory>();
        services.TryAddSingleton<IRepositoryProcessorService, RepositoryProcessorService>();
        services.TryAddSingleton<ICSharpPackageVersionResolver, CSharpPackageVersionResolver>();
        services.TryAddSingleton<IProjectAnalysisService, ProjectAnalysisService>();
        services.TryAddSingleton<IPackageRecommendationService, PackageRecommendationService>();

        return services;
    }
}
