using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.ReportGenerator;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Tests.DART.Core.DependencyInjection;

public class DartCoreCompositionRegistrationTests
{
    [Fact]
    public void AddDartCore_ShouldRegisterCoreAndAnalyzerServices()
    {
        var services = new ServiceCollection();

        services.AddDartCore();

        AssertContainsRegistrationFor<IAnalysisOrchestrator>(services);
        AssertContainsRegistrationFor<IBlackduckFindingCollector>(services);
        AssertContainsRegistrationFor<IBlackduckAnalyzer>(services);
        AssertContainsRegistrationFor<IEolAnalyzer>(services);
        AssertContainsRegistrationFor<IBlackduckReportGenerator>(services);
        AssertContainsRegistrationFor<IBlackduckApiService>(services);
        AssertContainsRegistrationFor<IEOLAnalysisService>(services);
    }

    [Fact]
    public void AddDartCore_ShouldRegisterReportGeneratorService()
    {
        var services = new ServiceCollection();

        services.AddDartCore();

        AssertContainsRegistrationFor<IReportGenerator>(services);
    }

    private static void AssertContainsRegistrationFor<TService>(IServiceCollection services)
        where TService : class
    {
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(TService));
    }
}
