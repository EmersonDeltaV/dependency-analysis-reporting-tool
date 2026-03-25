using DART.Core.Blackduck;
using DART.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Core.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IBlackduckFindingCollector, BlackduckFindingCollector>();
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        return services;
    }
}

