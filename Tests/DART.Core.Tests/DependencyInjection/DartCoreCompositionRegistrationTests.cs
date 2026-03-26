using DART.Core;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using DART.ReportGenerator;
using DART.ReportGenerator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DART.Tests.DART.Core.DependencyInjection;

public class DartCoreCompositionRegistrationTests
{
    [Fact]
    public void AddDartCore_ShouldRegisterOnlyOrchestrator()
    {
        var services = new ServiceCollection();

        services.AddDartCore();

        AssertContainsRegistrationFor<IAnalysisOrchestrator>(services);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IBlackduckAnalyzer));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IEolAnalyzer));
    }

    [Fact]
    public void AddBlackduckAnalysis_ShouldRegisterBlackduckServices()
    {
        var services = new ServiceCollection();

        services.AddBlackduckAnalysis();

        AssertContainsRegistrationFor<IBlackduckAnalyzer>(services);
        AssertContainsRegistrationFor<IBlackduckFindingCollector>(services);
        AssertContainsRegistrationFor<IBlackduckReportGenerator>(services);
        AssertContainsRegistrationFor<IBlackduckReportService>(services);
        AssertContainsRegistrationFor<IBlackduckApiService>(services);
        AssertContainsRegistrationFor<IFileService>(services);
    }

    [Fact]
    public void AddEolAnalysis_ShouldRegisterEolServices()
    {
        var services = new ServiceCollection();

        services.AddEolAnalysis();

        AssertContainsRegistrationFor<IEolAnalyzer>(services);
        AssertContainsRegistrationFor<IEOLAnalysisService>(services);
        AssertContainsRegistrationFor<INugetMetadataService>(services);
        AssertContainsRegistrationFor<INpmMetadataService>(services);
        AssertContainsRegistrationFor<IAzureDevOpsClientFactory>(services);
        AssertContainsRegistrationFor<IRepositoryProcessorService>(services);
        AssertContainsRegistrationFor<ICSharpPackageVersionResolver>(services);
        AssertContainsRegistrationFor<IProjectAnalysisService>(services);
        AssertContainsRegistrationFor<IPackageRecommendationService>(services);
    }

    [Fact]
    public void AddReportGenerator_ShouldRegisterReportGeneratorService()
    {
        var services = new ServiceCollection();

        services.AddReportGenerator();

        AssertContainsRegistrationFor<IReportGenerator>(services);
    }

    private static void AssertContainsRegistrationFor<TService>(IServiceCollection services)
        where TService : class
    {
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(TService));
    }
}
