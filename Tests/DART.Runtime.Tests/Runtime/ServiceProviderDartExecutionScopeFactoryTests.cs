using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.ReportGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DART.Runtime.Tests.Runtime;

[Trait("Category", "Unit")]
public sealed class ServiceProviderDartExecutionScopeFactoryTests
{
    [Fact]
    public void Create_ShouldResolveRequestSpecificOptions_AndRuntimeServices()
    {
        var factory = new ServiceProviderDartExecutionScopeFactory(NullLoggerFactory.Instance);
        var request = new DartExecutionRequest
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = "C:\\Temp\\Dart",
                LogPath = "C:\\Temp\\Dart\\Logs",
                ProductName = "DeltaV",
                ProductVersion = "v1.0",
                ProductIteration = "PI35"
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                BaseUrl = "https://blackduck.example.com",
                Token = "token-value"
            },
            EolAnalysisConfiguration = new EOLAnalysisConfig
            {
                Pat = "ado-pat",
                NuGetApiUrl = "https://api.nuget.org/v3/index.json",
                NpmRegistryUrl = "https://registry.npmjs.org"
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = true,
                EnableCSharpAnalysis = true
            }
        };

        using var scope = factory.Create(request);

        var blackduckOptions = scope.Services.GetRequiredService<IOptions<BlackduckConfiguration>>().Value;
        var eolOptions = scope.Services.GetRequiredService<IOptions<EOLAnalysisConfig>>().Value;
        var orchestrator = scope.Services.GetRequiredService<IAnalysisOrchestrator>();
        var reportGenerator = scope.Services.GetRequiredService<IReportGenerator>();

        Assert.Equal("https://blackduck.example.com", blackduckOptions.BaseUrl);
        Assert.Equal("ado-pat", eolOptions.Pat);
        Assert.NotNull(orchestrator);
        Assert.NotNull(reportGenerator);
    }
}