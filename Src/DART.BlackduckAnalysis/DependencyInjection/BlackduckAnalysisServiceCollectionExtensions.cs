using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DART.BlackduckAnalysis;

public static class BlackduckAnalysisServiceCollectionExtensions
{
    public static IServiceCollection AddBlackduckAnalysis(this IServiceCollection services)
    {
        services.TryAddSingleton<IBlackduckFindingCollector, BlackduckFindingCollector>();
        services.TryAddSingleton<IBlackduckReportGenerator, BlackduckReportGenerator>();
        services.TryAddSingleton<IBlackduckReportService, BlackduckReportService>();
        services.TryAddSingleton<IBlackduckApiService, BlackduckApiService>();
        services.TryAddSingleton<IFileService, FileService>();

        return services;
    }
}
