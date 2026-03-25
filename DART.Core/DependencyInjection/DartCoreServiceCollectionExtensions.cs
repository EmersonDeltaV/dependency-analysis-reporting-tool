using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DART.Core;

public static class DartCoreServiceCollectionExtensions
{
    public static IServiceCollection AddDartCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IBlackduckFindingCollector, BlackduckFindingCollector>();
        services.TryAddSingleton<IBlackduckAnalyzer, DartCoreBlackduckAnalyzerAdapter>();
        services.TryAddSingleton<IEolAnalyzer, DartCoreEolAnalyzerAdapter>();
        services.TryAddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();

        services.TryAddSingleton<IBlackduckReportGenerator, BlackduckReportGenerator>();
        services.TryAddSingleton<IBlackduckReportService, BlackduckReportService>();
        services.TryAddSingleton<IBlackduckApiService, BlackduckApiService>();
        services.TryAddSingleton<IFileService, FileService>();

        services.TryAddSingleton<IEOLAnalysisService, EOLAnalysisService>();
        services.TryAddSingleton<INugetMetadataService, NugetMetadataService>();
        services.TryAddSingleton<IAzureDevOpsClientFactory, AzureDevOpsClientFactory>();
        services.TryAddSingleton<IRepositoryProcessorService, RepositoryProcessorService>();
        services.TryAddSingleton<ICSharpPackageVersionResolver, CSharpPackageVersionResolver>();
        services.TryAddSingleton<IProjectAnalysisService, ProjectAnalysisService>();
        services.TryAddSingleton<IPackageRecommendationService, PackageRecommendationService>();
        services.TryAddSingleton<INpmMetadataService, NpmMetadataService>();

        RegisterReportGenerator(services);
        return services;
    }

    private static void RegisterReportGenerator(IServiceCollection services)
    {
        var reportServiceType = Type.GetType("DART.ReportGenerator.IReportGenerator, DART.ReportGenerator");
        var reportImplementationType = Type.GetType("DART.ReportGenerator.ReportGenerator, DART.ReportGenerator");

        if (reportServiceType is null || reportImplementationType is null)
        {
            throw new InvalidOperationException(
                "Could not register report generator. Add a reference to DART.ReportGenerator so AddDartCore can wire IReportGenerator.");
        }

        services.TryAddSingleton(reportServiceType, reportImplementationType);
    }
}
