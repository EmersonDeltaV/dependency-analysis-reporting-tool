using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DART.Core;

public static class DartCoreServiceCollectionExtensions
{
    public static IServiceCollection AddDartCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IBlackduckAnalyzer, DartCoreBlackduckAnalyzerAdapter>();
        services.TryAddSingleton<IEolAnalyzer, DartCoreEolAnalyzerAdapter>();
        services.TryAddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        return services;
    }
}
