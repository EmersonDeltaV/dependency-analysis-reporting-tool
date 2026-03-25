using DART.Core.Contracts;
using DART.Core.DependencyInjection;
using DART.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DART.Tests.DART.Core.Services;

public class AnalysisOrchestratorTests
{
    [Fact]
    public async Task RunAsync_ShouldOnlyExecuteEnabledAnalyzers()
    {
        var blackduckAnalyzer = Substitute.For<IBlackduckAnalyzer>();
        var eolAnalyzer = Substitute.For<IEolAnalyzer>();

        blackduckAnalyzer.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns([new BlackduckFinding { ApplicationName = "APP01" }]);

        var sut = new AnalysisOrchestrator(blackduckAnalyzer, eolAnalyzer);

        var result = await sut.RunAsync(
            new AnalysisRequest { EnableBlackduckAnalysis = true, EnableEolAnalysis = false },
            CancellationToken.None);

        await blackduckAnalyzer.Received(1).AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>());
        await eolAnalyzer.DidNotReceive().AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>());
        Assert.Single(result.BlackduckFindings);
        Assert.Empty(result.EolFindings);
        Assert.Equal(RunStatus.Completed, result.Status);
    }

    [Fact]
    public async Task RunAsync_ShouldReturnCompletedWithWarnings_WhenOneAnalyzerFails()
    {
        var blackduckAnalyzer = Substitute.For<IBlackduckAnalyzer>();
        var eolAnalyzer = Substitute.For<IEolAnalyzer>();

        blackduckAnalyzer.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IReadOnlyCollection<BlackduckFinding>>(new InvalidOperationException("Black Duck failure")));

        eolAnalyzer.AnalyzeAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns([new EolFinding { PackageId = "Pkg" }]);

        var sut = new AnalysisOrchestrator(blackduckAnalyzer, eolAnalyzer);

        var result = await sut.RunAsync(
            new AnalysisRequest { EnableBlackduckAnalysis = true, EnableEolAnalysis = true },
            CancellationToken.None);

        Assert.Equal(RunStatus.CompletedWithWarnings, result.Status);
        Assert.Single(result.Issues);
        Assert.True(result.Issues[0].IsWarning);
        Assert.Contains("Black Duck failure", result.Issues[0].Message);
        Assert.Single(result.EolFindings);
    }

    [Fact]
    public void AddCore_ShouldRegisterOrchestrator()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<IBlackduckAnalyzer>());
        services.AddSingleton(Substitute.For<IEolAnalyzer>());

        services.AddDartCore();

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetService<IAnalysisOrchestrator>();

        Assert.NotNull(orchestrator);
    }
}


