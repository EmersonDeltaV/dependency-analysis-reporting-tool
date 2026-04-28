using DART.BlackduckAnalysis;
using DART.Console;
using DART.Core;
using DART.EOLAnalysis;
using DART.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Config = DART.Console.Config;

namespace DART.Tests.DART.Console;

public class DartOrchestratorTests
{
    private readonly IDartExecutionRunner _executionRunner;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DartOrchestrator> _logger;

    public DartOrchestratorTests()
    {
        _executionRunner = Substitute.For<IDartExecutionRunner>();
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
            _executionRunner,
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
            _executionRunner,
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

        var sut = CreateOrchestrator(config);

        Assert.NotNull(sut);
    }

    [Fact]
    public async Task StartAsync_ShouldDelegateToRuntimeRunner_WithMappedRequest()
    {
        var config = new Config
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = @"C:\Output",
                LogPath = @"C:\Logs",
                ProductName = "Edge",
                ProductVersion = "v2.2",
                ProductIteration = "PI38"
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                BaseUrl = "https://blackduck.emrsn.org",
                Token = "token"
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = true,
                EnableCSharpAnalysis = true
            },
            EOLAnalysis = new EOLAnalysisConfig
            {
                Pat = "ado-pat"
            }
        };

        var sut = CreateOrchestrator(config);

        await sut.StartAsync(CancellationToken.None);

        await _executionRunner.Received(1).RunAsync(
            Arg.Is<DartExecutionRequest>(request =>
                request.ReportConfiguration.ProductName == "Edge"
                && request.BlackduckConfiguration.BaseUrl == "https://blackduck.emrsn.org"
                && request.EolAnalysisConfiguration.Pat == "ado-pat"),
            Arg.Any<IProgress<DartExecutionProgress>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_ShouldLogRuntimeIssuesAsWarnings()
    {
        var config = CreateConfig(enableBlackduck: false, enableCsharp: true, enableNpm: false);

        _executionRunner.RunAsync(Arg.Any<DartExecutionRequest>(), Arg.Any<IProgress<DartExecutionProgress>>(), Arg.Any<CancellationToken>())
            .Returns(new DartExecutionResult
            {
                Issues = [new RunIssue { Source = "EOL", Message = "Sample warning", IsWarning = true }],
                Status = RunStatus.CompletedWithWarnings
            });

        var sut = CreateOrchestrator(config);

        await sut.StartAsync(CancellationToken.None);

        _logger.ReceivedWithAnyArgs().Log<object>(LogLevel.Warning, default, default!, default!, default!);
    }

    [Fact]
    public async Task StartAsync_ShouldStopApplication_WhenRunnerThrows()
    {
        var config = CreateConfig(enableBlackduck: true, enableCsharp: false, enableNpm: false);
        _executionRunner.RunAsync(Arg.Any<DartExecutionRequest>(), Arg.Any<IProgress<DartExecutionProgress>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<DartExecutionResult>(new InvalidOperationException("boom")));

        var sut = CreateOrchestrator(config);

        await sut.StartAsync(CancellationToken.None);

        _logger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        _lifetime.Received(1).StopApplication();
    }

    private DartOrchestrator CreateOrchestrator(Config config)
        => new(
            Options.Create(config),
            _executionRunner,
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
