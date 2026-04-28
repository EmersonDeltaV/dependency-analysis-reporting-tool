using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DART.Runtime;

public static class DartRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddDartRuntime(this IServiceCollection services)
    {
        services.TryAddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
        services.TryAddSingleton<IDartExecutionScopeFactory, ServiceProviderDartExecutionScopeFactory>();

        return services;
    }
}