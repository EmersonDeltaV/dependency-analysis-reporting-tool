using DART.Core.Blackduck;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Core;

public static class DartCoreServiceCollectionExtensions
{
    public static IServiceCollection AddDartCore(this IServiceCollection services)
    {
        services.AddSingleton<IBlackduckFindingCollector, BlackduckFindingCollector>();
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        return services;
    }
}
