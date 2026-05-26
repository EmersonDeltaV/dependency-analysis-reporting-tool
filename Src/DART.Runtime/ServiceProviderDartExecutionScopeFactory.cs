using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.ReportGenerator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DART.Runtime;

public sealed class ServiceProviderDartExecutionScopeFactory : IDartExecutionScopeFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public ServiceProviderDartExecutionScopeFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IDartExecutionScope Create(DartExecutionRequest request)
    {
        var services = new ServiceCollection();

        services.AddSingleton(_loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        services.AddBlackduckAnalysis();
        services.AddEolAnalysis();
        services.AddReportGenerator();

        services.AddSingleton<IOptions<BlackduckConfiguration>>(Options.Create(request.BlackduckConfiguration));
        services.AddSingleton<IOptions<EOLAnalysisConfig>>(Options.Create(request.EolAnalysisConfiguration));
        services.AddSingleton<IOptions<ReportConfiguration>>(Options.Create(request.ReportConfiguration));
        services.AddSingleton<IOptions<FeatureToggles>>(Options.Create(request.FeatureToggles));

        return new ServiceProviderDartExecutionScope(services.BuildServiceProvider(validateScopes: true));
    }
}