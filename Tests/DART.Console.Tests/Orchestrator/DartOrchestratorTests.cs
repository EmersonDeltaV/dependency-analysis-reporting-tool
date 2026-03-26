using DART.BlackduckAnalysis;
using DART.Console;
using DART.Core;
using DART.EOLAnalysis;
using DART.Console.Exceptions;
using DART.ReportGenerator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Config = DART.Console.Config;

namespace DART.Tests.DART.Console;

public class DartOrchestratorTests
{
    private readonly IAnalysisOrchestrator _coreAnalysisOrchestrator;
    private readonly IReportGenerator _reportGenerator;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DartOrchestrator> _logger;

    public DartOrchestratorTests()
    {
        _coreAnalysisOrchestrator = Substitute.For<IAnalysisOrchestrator>();
        _reportGenerator = Substitute.For<IReportGenerator>();
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _logger = Substitute.For<ILogger<DartOrchestrator>>();
    }

    [Fact]
    public void Constructor_ShouldThrowConfigException_WhenConfigurationIsNull()
    {
        var nullOptions = Substitute.For<IOptions<Config>>();
        nullOptions.Value.Returns((Config)null!);

        var exception = Assert.Throws<ConfigException>(() => new DartOrchestrator(
            nullOptions,
            _coreAnalysisOrchestrator,
            _reportGenerator,
            _lifetime,
            _logger));

        Assert.Equal("Failed to load configuration", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowConfigException_WhenRequiredFieldsMissingAndBlackduckEnabled()
    {
        var invalidConfig = new Config
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = "",
                ProductName = "",
                ProductVersion = ""
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                BaseUrl = "",
                Token = ""
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = true
            },
            EOLAnalysis = new EOLAnalysisConfig()
        };

        var exception = Assert.Throws<ConfigException>(() => new DartOrchestrator(
            Options.Create(invalidConfig),
            _coreAnalysisOrchestrator,
            _reportGenerator,
            _lifetime,
            _logger));

        Assert.Contains("BlackduckConfiguration:BaseUrl is required", exception.Message);
        Assert.Contains("BlackduckConfiguration:Token is required", exception.Message);
        Assert.Contains("ProductName is required", exception.Message);
        Assert.Contains("ProductVersion is required", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldNotRequireBlackduckFields_WhenBlackduckDisabled()
    {
        var config = CreateConfig(enableBlackduck: false, enableCsharp: false, enableNpm: false);
        config.BlackduckConfiguration.BaseUrl = string.Empty;
        config.BlackduckConfiguration.Token = string.Empty;

        var sut = CreateProgram(config);

        Assert.NotNull(sut);
    }

    [Fact]
    public async Task StartAsync_ShouldCompareCurrentWithPrevious_WhenBothResultsProvided()
    {
        var config = CreateConfig(enableBlackduck: true, enableCsharp: true, enableNpm: false);
        config.BlackduckConfiguration.PreviousResults = @"C:\Previous\report.xlsx";
        config.BlackduckConfiguration.CurrentResults = @"C:\Current\report.xlsx";

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        _reportGenerator.Received(1).CompareCurrentWithPrevious(
            config.BlackduckConfiguration.CurrentResults,
            config.BlackduckConfiguration.PreviousResults);

        await _coreAnalysisOrchestrator.DidNotReceive().RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldCallCoreAndGenerateReportWithoutEolOverload_WhenNoEolFindings()
    {
        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
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

        _reportGenerator.GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(@"C:\Output\report.xlsx");

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        await _coreAnalysisOrchestrator.Received(1).RunAsync(
            Arg.Is<AnalysisRequest>(r => r.EnableBlackduckAnalysis && !r.EnableEolAnalysis),
            Arg.Any<CancellationToken>());

        _reportGenerator.Received(1).GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());

        _reportGenerator.DidNotReceive().GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<IReadOnlyCollection<EolFinding>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task StartAsync_ShouldUseEolOverload_WhenEolFindingsExist()
    {
        var config = CreateConfig(enableBlackduck: false, enableCsharp: true, enableNpm: false);

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult
            {
                EolFindings =
                [
                    new EolFinding
                    {
                        PackageId = "Newtonsoft.Json",
                        Repository = "Repo",
                        Project = "Proj",
                        CurrentVersion = "12.0.3"
                    }
                ]
            });

        _reportGenerator.GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<IReadOnlyCollection<EolFinding>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(@"C:\Output\report.xlsx");

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        await _coreAnalysisOrchestrator.Received(1).RunAsync(
            Arg.Is<AnalysisRequest>(r => !r.EnableBlackduckAnalysis && r.EnableEolAnalysis),
            Arg.Any<CancellationToken>());

        _reportGenerator.Received(1).GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<IReadOnlyCollection<EolFinding>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task StartAsync_ShouldSetCurrentResults_WhenBlackduckEnabledAndCurrentResultsMissing()
    {
        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);
        config.BlackduckConfiguration.CurrentResults = string.Empty;

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult());

        _reportGenerator.GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(@"C:\Output\generated.xlsx");

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        Assert.Equal(@"C:\Output\generated.xlsx", config.BlackduckConfiguration.CurrentResults);
    }

    [Fact]
    public async Task StartAsync_ShouldNotGenerateReport_WhenAllAnalysisTogglesDisabled()
    {
        var config = CreateConfig(enableBlackduck: false, enableCsharp: false, enableNpm: false);

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult());

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        await _coreAnalysisOrchestrator.Received(1).RunAsync(
            Arg.Is<AnalysisRequest>(r => !r.EnableBlackduckAnalysis && !r.EnableEolAnalysis),
            Arg.Any<CancellationToken>());

        _reportGenerator.DidNotReceive().GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());

        _reportGenerator.DidNotReceive().GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<IReadOnlyCollection<EolFinding>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task StartAsync_ShouldLogCoreIssuesAsWarnings()
    {
        var config = CreateConfig(enableBlackduck: false, enableCsharp: true, enableNpm: false);

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AnalysisResult
            {
                Issues =
                [
                    new RunIssue
                    {
                        Source = "EOL",
                        Message = "Sample warning",
                        IsWarning = true
                    }
                ],
                EolFindings = [new EolFinding { PackageId = "pkg" }]
            });

        _reportGenerator.GenerateCurrentFormatReport(
            Arg.Any<IReadOnlyCollection<RowDetails>>(),
            Arg.Any<IReadOnlyCollection<EolFinding>>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(@"C:\Output\report.xlsx");

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        _logger.ReceivedWithAnyArgs().Log<object>(LogLevel.Warning, default, default!, default!, default!);
    }

    [Fact]
    public async Task StartAsync_ShouldStopApplication_WhenCoreThrows()
    {
        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);

        _coreAnalysisOrchestrator.RunAsync(Arg.Any<AnalysisRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<AnalysisResult>(new InvalidOperationException("boom")));

        var sut = CreateProgram(config);

        await sut.StartAsync(CancellationToken.None);

        _logger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        _lifetime.Received(1).StopApplication();
    }

    private DartOrchestrator CreateProgram(Config config)
        => new(
            Options.Create(config),
            _coreAnalysisOrchestrator,
            _reportGenerator,
            _lifetime,
            _logger);

    private static Config CreateConfig(bool enableBlackduck, bool enableCsharp, bool enableNpm)
        => new()
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = @"C:\Output",
                LogPath = @"C:\Logs",
                ProductName = "TestProduct",
                ProductVersion = "1.0",
                ProductIteration = "1"
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                BaseUrl = "https://test.blackduck.com",
                Token = "test-token",
                PreviousResults = string.Empty,
                CurrentResults = string.Empty
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
