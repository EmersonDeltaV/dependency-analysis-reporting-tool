using DART.Core.Blackduck;
using DART.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Core.DependencyInjection;

public static class DartCoreServiceCollectionExtensions
{
    public static IServiceCollection AddDartCore(this IServiceCollection services)
    {
        services.AddSingleton<IBlackduckFindingCollector, BlackduckFindingCollector>();
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        return services;
    }
}

