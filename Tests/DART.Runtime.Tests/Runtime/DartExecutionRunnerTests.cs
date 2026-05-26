using DART.Core;
using DART.ReportGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DART.Runtime.Tests.Runtime;

[Trait("Category", "Unit")]
public sealed class DartExecutionRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldCompareReports_WhenCurrentAndPreviousResultsArePresent()
    {
        var scopeFactory = Substitute.For<IDartExecutionScopeFactory>();
        var scope = Substitute.For<IDartExecutionScope>();
        var services = new ServiceCollection();
        var orchestrator = Substitute.For<IAnalysisOrchestrator>();
        var reportGenerator = Substitute.For<IReportGenerator>();
        var progressEvents = new List<DartExecutionProgress>();

        services.AddSingleton(orchestrator);
        services.AddSingleton(reportGenerator);
        scope.Services.Returns(services.BuildServiceProvider());
        scopeFactory.Create(Arg.Any<DartExecutionRequest>()).Returns(scope);

        var sut = new DartExecutionRunner(scopeFactory, NullLogger<DartExecutionRunner>.Instance);
        var request = new DartExecutionRequest
        {
            BlackduckConfiguration = new DART.BlackduckAnalysis.BlackduckConfiguration
            {
                CurrentResults = @"C:\Current\report.xlsx",
                PreviousResults = @"C:\Previous\report.xlsx"
            }
        };

        var result = await sut.RunAsync(request, new Progress<DartExecutionProgress>(progressEvents.Add), CancellationToken.None);

        reportGenerator.Received(1).CompareCurrentWithPrevious(@"C:\Current\report.xlsx", @"C:\Previous\report.xlsx");
        await orchestrator.DidNotReceive().RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>());
        Assert.Equal(RunStatus.Completed, result.Status);
        Assert.Contains(progressEvents, item => item.Stage == DartExecutionStage.ComparingReports);
        Assert.Contains(progressEvents, item => item.Stage == DartExecutionStage.Completed);
    }

    [Fact]
    public async Task RunAsync_ShouldDisposeScopeAsynchronously()
    {
        var scopeFactory = Substitute.For<IDartExecutionScopeFactory>();
        var services = new ServiceCollection();
        var reportGenerator = Substitute.For<IReportGenerator>();
        var trackingScope = new TrackingExecutionScope(services.BuildServiceProvider());

        services.AddSingleton(Substitute.For<IAnalysisOrchestrator>());
        services.AddSingleton(reportGenerator);

        trackingScope = new TrackingExecutionScope(services.BuildServiceProvider());
        scopeFactory.Create(Arg.Any<DartExecutionRequest>()).Returns(trackingScope);

        var sut = new DartExecutionRunner(scopeFactory, NullLogger<DartExecutionRunner>.Instance);
        var request = new DartExecutionRequest
        {
            BlackduckConfiguration = new DART.BlackduckAnalysis.BlackduckConfiguration
            {
                CurrentResults = @"C:\Current\report.xlsx",
                PreviousResults = @"C:\Previous\report.xlsx"
            }
        };

        await sut.RunAsync(request, progress: null, CancellationToken.None);

        Assert.False(trackingScope.DisposeCalled);
        Assert.True(trackingScope.DisposeAsyncCalled);
    }

    [Fact]
    public async Task RunAsync_ShouldGenerateWorkbookAndEmitProgress_WhenAnalysisIsEnabled()
    {
        var scopeFactory = Substitute.For<IDartExecutionScopeFactory>();
        var scope = Substitute.For<IDartExecutionScope>();
        var services = new ServiceCollection();
        var orchestrator = Substitute.For<IAnalysisOrchestrator>();
        var reportGenerator = Substitute.For<IReportGenerator>();
        var progressEvents = new List<DartExecutionProgress>();

        services.AddSingleton(orchestrator);
        services.AddSingleton(reportGenerator);
        scope.Services.Returns(services.BuildServiceProvider());
        scopeFactory.Create(Arg.Any<DartExecutionRequest>()).Returns(scope);

        orchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult
            {
                Status = RunStatus.CompletedWithWarnings,
                Issues = [new RunIssue { Source = "EOL", Message = "warning", IsWarning = true }],
                EolFindings = [new EolFinding { PackageId = "Newtonsoft.Json", Repository = "Repo", Project = "Project", CurrentVersion = "13.0.3" }]
            });

        reportGenerator.GenerateCurrentFormatReport(
                Arg.Any<IReadOnlyCollection<RowDetails>>(),
                Arg.Any<IReadOnlyCollection<EolFinding>>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(@"C:\Output\dart-summary-iscm.xlsx");

        var sut = new DartExecutionRunner(scopeFactory, NullLogger<DartExecutionRunner>.Instance);
        var request = new DartExecutionRequest
        {
            AppCode = "iscm",
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = @"C:\Output",
                ProductName = "DeltaV",
                ProductVersion = "v1.0",
                ProductIteration = "PI35"
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = false,
                EnableCSharpAnalysis = true,
                EnableNpmAnalysis = false
            }
        };

        var result = await sut.RunAsync(request, new Progress<DartExecutionProgress>(progressEvents.Add), CancellationToken.None);

        Assert.Equal(RunStatus.CompletedWithWarnings, result.Status);
        Assert.Contains(progressEvents, item => item.Stage == DartExecutionStage.RunningEolAnalysis);
        Assert.Contains(progressEvents, item => item.Stage == DartExecutionStage.GeneratingWorkbook);
        Assert.Contains(progressEvents, item => item.Stage == DartExecutionStage.CompletedWithWarnings);
    }

    private sealed class TrackingExecutionScope : IDartExecutionScope
    {
        public TrackingExecutionScope(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public bool DisposeCalled { get; private set; }

        public bool DisposeAsyncCalled { get; private set; }

        public void Dispose()
        {
            DisposeCalled = true;
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }
}