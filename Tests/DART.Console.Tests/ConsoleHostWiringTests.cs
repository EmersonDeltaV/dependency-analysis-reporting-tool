using DART.Console;
using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.ReportGenerator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Config = DART.Console.Config;

namespace DART.Tests.DART.Console;

public class ConsoleHostWiringTests
{
    [Fact]
    public async Task StartAsync_ShouldInvokeCoreThenReportGenerator_WhenAnyAnalysisToggleEnabled()
    {
        var coreOrchestrator = Substitute.For<IAnalysisOrchestrator>();
        var reportGenerator = Substitute.For<IReportGenerator>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var logger = Substitute.For<ILogger<DartOrchestrator>>();

        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);
        var options = Options.Create(config);

        coreOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult
            {
                BlackduckFindings =
                [
                    new BlackduckFinding
                    {
                        ApplicationName = "APP01",
                        SoftwareComponent = "Newtonsoft.Json",
                        Version = "13.0.3",
                        SecurityRisk = "HIGH",
                        VulnerabilityId = "CVE-1",
                        RecommendedFix = "13.0.4",
                        MatchType = "Direct Dependency"
                    }
                ]
            });

        var sut = new DartOrchestrator(
            options,
            coreOrchestrator,
            reportGenerator,
            lifetime,
            logger);

        await sut.StartAsync(CancellationToken.None);

        Received.InOrder(() =>
        {
            coreOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>());
            reportGenerator.GenerateCurrentFormatReport(
                Arg.Any<IReadOnlyCollection<RowDetails>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>());
        });

        await coreOrchestrator.Received(1).RunAsync(
            Arg.Is<AnalysisRequest>(r => r.EnableBlackduckAnalysis && !r.EnableEolAnalysis),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldSkipReportGenerator_WhenAllAnalysisTogglesDisabled()
    {
        var coreOrchestrator = Substitute.For<IAnalysisOrchestrator>();
        var reportGenerator = Substitute.For<IReportGenerator>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var logger = Substitute.For<ILogger<DartOrchestrator>>();

        var config = CreateConfig(enableBlackduck: false, enableCsharp: false, enableNpm: false);
        var options = Options.Create(config);

        coreOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult());

        var sut = new DartOrchestrator(
            options,
            coreOrchestrator,
            reportGenerator,
            lifetime,
            logger);

        await sut.StartAsync(CancellationToken.None);

        reportGenerator.DidNotReceive().GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    private static Config CreateConfig(bool enableBlackduck, bool enableCsharp, bool enableNpm)
    {
        return new Config
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = "C:\\Output",
                LogPath = "C:\\Logs",
                ProductName = "Product",
                ProductVersion = "1.0",
                ProductIteration = "Sprint1"
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                Token = "token",
                BaseUrl = "https://example.blackduck.com"
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = enableBlackduck,
                EnableCSharpAnalysis = enableCsharp,
                EnableNpmAnalysis = enableNpm
            },
            EOLAnalysis = new EOLAnalysisConfig()
        };
    }
}
