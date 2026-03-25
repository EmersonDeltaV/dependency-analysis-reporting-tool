using DART.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Core.DependencyInjection;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        return services;
    }
}

