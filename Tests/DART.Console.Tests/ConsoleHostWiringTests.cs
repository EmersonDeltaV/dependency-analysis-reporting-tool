using DART.Console;
using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Config = DART.Console.Config;

namespace DART.Tests.DART.Console;

public class ConsoleHostWiringTests
{
    [Fact]
    public async Task StartAsync_ShouldInvokeRuntimeRunner_WhenAnyAnalysisToggleEnabled()
    {
        var runner = Substitute.For<IDartExecutionRunner>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var logger = Substitute.For<ILogger<DartOrchestrator>>();

        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);
        var options = Options.Create(config);

        runner.RunAsync(Arg.Any<DartExecutionRequest>(), Arg.Any<IProgress<DartExecutionProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new DartExecutionResult());

        var sut = new DartOrchestrator(
            options,
            runner,
            lifetime,
            logger);

        await sut.StartAsync(CancellationToken.None);

        await runner.Received(1).RunAsync(
            Arg.Is<DartExecutionRequest>(request =>
                request.FeatureToggles.EnableBlackduckAnalysis
                && !request.FeatureToggles.EnableCSharpAnalysis
                && !request.FeatureToggles.EnableNpmAnalysis),
            Arg.Any<IProgress<DartExecutionProgress>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldInvokeRuntimeRunner_WhenAllAnalysisTogglesDisabled()
    {
        var runner = Substitute.For<IDartExecutionRunner>();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var logger = Substitute.For<ILogger<DartOrchestrator>>();

        var config = CreateConfig(enableBlackduck: false, enableCsharp: false, enableNpm: false);
        var options = Options.Create(config);

        runner.RunAsync(Arg.Any<DartExecutionRequest>(), Arg.Any<IProgress<DartExecutionProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new DartExecutionResult());

        var sut = new DartOrchestrator(
            options,
            runner,
            lifetime,
            logger);

        await sut.StartAsync(CancellationToken.None);

        await runner.Received(1).RunAsync(
            Arg.Is<DartExecutionRequest>(request =>
                !request.FeatureToggles.EnableBlackduckAnalysis
                && !request.FeatureToggles.EnableCSharpAnalysis
                && !request.FeatureToggles.EnableNpmAnalysis),
            Arg.Any<IProgress<DartExecutionProgress>>(),
            Arg.Any<CancellationToken>());
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
