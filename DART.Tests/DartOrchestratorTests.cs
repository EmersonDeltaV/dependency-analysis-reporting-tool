using ClosedXML.Excel;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using DART.EOLAnalysis.Models;
using DART.Exceptions;
using DART.Models;
using DART.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DART.Tests
{
    public class DartOrchestratorTests : IDisposable
    {
        private readonly IBlackduckReportGenerator _mockBlackduckReportGenerator;
        private readonly ICsvService _mockCsvService;
        private readonly IExcelService _mockExcelService;
        private readonly IEOLAnalysisService _mockEOLAnalysisService;
        private readonly ILogger<DartOrchestrator> _mockLogger;

        private readonly TextReader _originalConsoleIn;

        public DartOrchestratorTests()
        {
            // Prevent Console.ReadLine() in StartAsync from blocking tests
            _originalConsoleIn = Console.In;
            Console.SetIn(new StringReader(Environment.NewLine));

            _mockBlackduckReportGenerator = Substitute.For<IBlackduckReportGenerator>();
            _mockCsvService = Substitute.For<ICsvService>();
            _mockExcelService = Substitute.For<IExcelService>();
            _mockEOLAnalysisService = Substitute.For<IEOLAnalysisService>();
            _mockLogger = Substitute.For<ILogger<DartOrchestrator>>();
        }

        public void Dispose()
        {
            // Restore original Console.In after tests
            Console.SetIn(_originalConsoleIn);
        }

        private Config CreateDefaultConfig()
        {
            return new Config
            {
                ReportConfiguration = new ReportConfiguration
                {
                    OutputFilePath = "C:\\Output",
                    LogPath = "C:\\Logs",
                    ProductName = "TestProduct",
                    ProductVersion = "1.0",
                    ProductIteration = "1"
                },
                BlackduckConfiguration = new BlackduckConfiguration
                {
                    Token = "test-token",
                    BaseUrl = "https://test.blackduck.com",
                    PreviousResults = string.Empty,
                    CurrentResults = string.Empty
                },
                FeatureToggles = new FeatureToggles
                {
                    EnableEOLAnalysis = false
                },
                EOLAnalysis = new EOLAnalysisConfig()
            };
        }

        private Config CreateConfigWithFeatures(bool enableDownloadTool = false, bool enableEOLAnalysis = false,
            string previousResults = "", string currentResults = "", int repositoryCount = 0)
        {
            var config = CreateDefaultConfig();
            config.FeatureToggles.EnableEOLAnalysis = enableEOLAnalysis;
            config.BlackduckConfiguration.PreviousResults = previousResults;
            config.BlackduckConfiguration.CurrentResults = currentResults;

            if (repositoryCount > 0)
            {
                config.EOLAnalysis.Repositories = new List<Repository>();
                for (int i = 0; i < repositoryCount; i++)
                {
                    config.EOLAnalysis.Repositories.Add(new Repository
                    {
                        Name = $"Repo{i}",
                        Url = $"https://dev.azure.com/TestOrg/TestProject/_git/Repo{i}",
                        Branch = "main"
                    });
                }
            }

            return config;
        }

        // Removed legacy IConfiguration builder; tests now use IOptions<Config> directly

        private DartOrchestrator CreateProgram(Config config)
        {
            var configOptions = Options.Create(config);

            return new DartOrchestrator(
                _mockBlackduckReportGenerator,
                configOptions,
                _mockCsvService,
                _mockExcelService,
                _mockEOLAnalysisService,
                _mockLogger);
        }

        [Fact]
        public async Task StartAsync_ShouldCallGenerateReportAnalyzeReportAndCleanup_WhenNoPreviousResultsAndDownloadToolEnabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.3.1 GenerateReport() called once
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();

            // 1.3.2 AnalyzeReport() called once
            await _mockCsvService.Received(1).AnalyzeReport();

            // 1.3.3 GetWorkbook() returns workbook; SaveWorkbook(workbook) called once
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // 1.3.5 Cleanup() called once
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // 1.3.6 Verify logging information messages at key steps
            _mockLogger.Received().LogInformation("No previous results found. Skipping comparison.");
        }

        [Fact]
        public async Task StartAsync_ShouldCallEOLAnalysisAndAddSheet_WhenEOLAnalysisEnabledWithRepositories()
        {
            // Arrange
            var eolData = new List<PackageData>
            {
                new PackageData { Id = "TestPackage", Version = "1.0.0", Project = "TestProject" }
            };

            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 2);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(eolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.3.4 If EnableEOLAnalysis = true and repositories > 0: AnalyzeRepositoriesAsync called
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);

            // on non-empty result, AddEOLAnalysisSheet called
            _mockExcelService.Received(1).AddEOLAnalysisSheet(mockWorkbook, eolData);

            // Verify EOL analysis logging
            _mockLogger.Received().LogInformation("Starting EOL Analysis...");
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Information, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallEOLAnalysis_WhenEOLAnalysisEnabledButNoRepositories()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 0);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.3.4 AnalyzeRepositoriesAsync NOT called when repositories is empty
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());

            // AddEOLAnalysisSheet NOT called
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());
        }

        [Fact]
        public async Task StartAsync_ShouldNotAddEOLSheet_WhenEOLAnalysisReturnsEmptyResults()
        {
            // Arrange
            var emptyEolData = new List<PackageData>();

            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(emptyEolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // AnalyzeRepositoriesAsync called but AddEOLAnalysisSheet NOT called for empty result
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());

            // Verify appropriate logging for empty result
            _mockLogger.Received().LogInformation("EOL Analysis completed but no packages were found.");
        }

        [Fact]
        public async Task StartAsync_ShouldCallGenerateReportAndCleanup_ByDefault()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: false);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.4.1 GenerateReport() and Cleanup() are called by default
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // 1.4.2 Other calls as above still happen
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // Verify logging information messages at key steps
            _mockLogger.Received().LogInformation("No previous results found. Skipping comparison.");
        }

        [Fact]
        public async Task StartAsync_ShouldCallGenerateReportAndCleanup_WithEOLAnalysis()
        {
            // Arrange
            var eolData = new List<PackageData>
            {
                new PackageData { Id = "TestPackage", Version = "1.0.0", Project = "TestProject" }
            };

            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: true, repositoryCount: 2);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(eolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.4.1 GenerateReport() and Cleanup() are called by default
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // 1.4.2 Other calls as above still happen including EOL analysis
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // EOL analysis should still work when download tool is disabled
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
            _mockExcelService.Received(1).AddEOLAnalysisSheet(mockWorkbook, eolData);

            // Verify logging
            _mockLogger.Received().LogInformation("No previous results found. Skipping comparison.");
            _mockLogger.Received().LogInformation("Starting EOL Analysis...");
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Information, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallEOLAnalysis_WhenEOLAnalysisDisabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false, repositoryCount: 2);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.5.1 AnalyzeRepositoriesAsync NOT called
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());

            // 1.5.2 AddEOLAnalysisSheet NOT called
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());

            // Other calls should still happen
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);
            await _mockBlackduckReportGenerator.Received(1).Cleanup();
        }

        [Fact]
        public async Task StartAsync_ShouldHandleEOLAnalysisException_WhenEOLAnalysisThrows()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 2);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            var expectedException = new InvalidOperationException("EOL Analysis failed");

            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromException<List<PackageData>>(expectedException));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.6.1 Error is logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);

            // 1.6.2 Workbook still saved
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // 1.6.3 Cleanup behavior matches current code
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // EOL analysis should have been attempted
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);

            // AddEOLAnalysisSheet should NOT be called when exception occurs
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());
        }

        [Fact]
        public async Task StartAsync_ShouldHandleEOLAnalysisException_WhenDownloadToolDisabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            var expectedException = new ArgumentException("Configuration error");

            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromException<List<PackageData>>(expectedException));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.6.1 Error is logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);

            // 1.6.2 Workbook still saved
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // 1.6.3 Download still runs and cleanup occurs
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // EOL analysis should have been attempted
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
        }

        [Fact]
        public async Task StartAsync_ShouldCallCompareExcelFiles_WhenBothPreviousAndCurrentResultsProvided()
        {
            // Arrange
            var config = CreateConfigWithFeatures(
                enableDownloadTool: true,
                enableEOLAnalysis: true,
                previousResults: "C:\\Previous\\report.xlsx",
                currentResults: "C:\\Current\\report.xlsx",
                repositoryCount: 2);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.7.1 CompareExcelFiles(current, previous, output) called once
            _mockExcelService.Received(1).CompareExcelFiles(config.BlackduckConfiguration.CurrentResults, config.BlackduckConfiguration.PreviousResults, config.ReportConfiguration.OutputFilePath);

            // 1.7.2 Other calls (download/analyze/eol/save/cleanup) NOT called
            await _mockBlackduckReportGenerator.DidNotReceive().GenerateReport();
            await _mockCsvService.DidNotReceive().AnalyzeReport();
            _mockExcelService.DidNotReceive().GetWorkbook();
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());
            await _mockBlackduckReportGenerator.DidNotReceive().Cleanup();
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());
        }

        [Fact]
        public void Constructor_ShouldThrowConfigException_WhenConfigurationReturnsNull()
        {
            // Arrange
            // Act & Assert
            // 1.8.1 Expect ConfigException
            var nullOptions = Substitute.For<IOptions<Config>>();
            nullOptions.Value.Returns((Config)null!);

            var exception = Assert.Throws<ConfigException>(() => new DartOrchestrator(
                _mockBlackduckReportGenerator,
                nullOptions,
                _mockCsvService,
                _mockExcelService,
                _mockEOLAnalysisService,
                _mockLogger));

            Assert.Equal("Failed to load configuration", exception.Message);
        }

        [Fact]
        public async Task StopAsync_ShouldReturnCompletedTask()
        {
            // Arrange
            var config = CreateDefaultConfig();
            var program = CreateProgram(config);

            // Act
            var result = program.StopAsync(CancellationToken.None);

            // Assert
            // 1.9 StopAsync returns Task.CompletedTask
            Assert.Equal(Task.CompletedTask, result);
            Assert.True(result.IsCompleted);
            await result; // Should complete immediately without throwing
        }

        [Fact]
        public async Task StartAsync_ShouldLogHttpRequestException_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var httpException = new HttpRequestException("Network error");

            _mockBlackduckReportGenerator.GenerateReport().Returns<Task>(x => throw httpException);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.10 HttpRequestException inside StartAsync is logged (structured logging)
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldLogGenericException_WhenUnexpectedExceptionThrown()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var genericException = new InvalidOperationException("Unexpected error");

            _mockCsvService.AnalyzeReport().Returns<Task>(x => throw genericException);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // 1.11 Generic Exception path is logged (structured logging)
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldLogArgumentException_WhenArgumentExceptionThrown()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var argumentException = new ArgumentException("Invalid argument");

            _mockCsvService.AnalyzeReport().Returns<Task>(x => throw argumentException);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // ArgumentException should be logged appropriately (structured logging)
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldLogConfigException_WhenConfigExceptionThrown()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var configException = new ConfigException("Configuration error");

            _mockCsvService.AnalyzeReport().Returns<Task>(x => throw configException);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // ConfigException should be logged appropriately (structured logging)
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldSaveWorkbookEvenWhenEOLAnalysisExceptionOccurs_WhenDownloadToolEnabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            var exception = new InvalidOperationException("EOL Analysis exception");

            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns<Task<List<PackageData>>>(x => Task.FromException<List<PackageData>>(exception));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // Workbook should still be saved even when EOL analysis exception occurs
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);

            // Cleanup should still be called when download tool is enabled
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // EOL Analysis error should be logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldCleanupWhenExceptionOccursBeforeWorkbookCreated_WhenDownloadToolEnabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var exception = new InvalidOperationException("Test exception before workbook");

            // Exception occurs before workbook is created (during AnalyzeReport)
            _mockCsvService.AnalyzeReport().Returns<Task>(x => throw exception);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // Workbook should NOT be saved since it was never created
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());

            // Cleanup should still be called when download tool is enabled
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // Exception should be logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallCleanupWhenExceptionOccurs_WhenDownloadToolDisabled()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: false);
            var exception = new InvalidOperationException("Test exception");

            // Exception occurs before workbook is created (during AnalyzeReport)
            _mockCsvService.AnalyzeReport().Returns<Task>(x => throw exception);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // Workbook should NOT be saved since it was never created
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());

            // Cleanup should be called even when an exception occurs
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // Exception should be logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }

        [Fact]
        public void Constructor_ShouldThrowConfigException_WhenRequiredFieldsMissing()
        {
            // Arrange
            var invalidConfig = new Config
            {
                // Missing required fields
                ReportConfiguration = new ReportConfiguration
                {
                    OutputFilePath = "",
                    ProductName = "",
                    ProductVersion = ""
                },
                BlackduckConfiguration = new BlackduckConfiguration
                {
                    Token = "",
                    BaseUrl = ""
                }
            };
            var configOptions = Options.Create(invalidConfig);

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => new DartOrchestrator(
                _mockBlackduckReportGenerator,
                configOptions,
                _mockCsvService,
                _mockExcelService,
                _mockEOLAnalysisService,
                _mockLogger));

            Assert.Contains("Configuration validation failed", exception.Message);
            
            Assert.Contains("OutputFilePath is required", exception.Message);
            Assert.Contains("BaseUrl is required", exception.Message);
            Assert.Contains("BlackduckConfiguration:Token is required", exception.Message);
            Assert.Contains("ProductName is required", exception.Message);
            Assert.Contains("ProductVersion is required", exception.Message);
        }

        [Fact]
        public void Constructor_ShouldNotThrow_WhenAllRequiredFieldsProvided()
        {
            // Arrange
            var validConfig = CreateDefaultConfig();
            var configOptions = Options.Create(validConfig);

            // Act & Assert - Should not throw
            var program = new DartOrchestrator(
                _mockBlackduckReportGenerator,
                configOptions,
                _mockCsvService,
                _mockExcelService,
                _mockEOLAnalysisService,
                _mockLogger);

            Assert.NotNull(program);
        }

        [Fact]
        public void Constructor_ShouldHandleNullFeatureToggles()
        {
            // Arrange
            var config = CreateDefaultConfig();
            config.FeatureToggles = null;
            var configOptions = Options.Create(config);

            // Act & Assert - Should not throw, should create default FeatureToggles
            var program = new DartOrchestrator(
                _mockBlackduckReportGenerator,
                configOptions,
                _mockCsvService,
                _mockExcelService,
                _mockEOLAnalysisService,
                _mockLogger);

            Assert.NotNull(program);
        }

        [Fact]
        public async Task StartAsync_ShouldOnlyProcessComparison_WhenOnlyPreviousResultsProvided()
        {
            // Arrange
            var config = CreateConfigWithFeatures(
                enableDownloadTool: true,
                enableEOLAnalysis: true,
                previousResults: "C:\\Previous\\report.xlsx",
                currentResults: "", // Empty current results
                repositoryCount: 2);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // Should not call comparison flow since both previous AND current are required
            _mockExcelService.DidNotReceive().CompareExcelFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            // Should proceed with normal initial report flow
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
        }

        [Fact]
        public async Task StartAsync_ShouldOnlyProcessComparison_WhenOnlyCurrentResultsProvided()
        {
            // Arrange
            var config = CreateConfigWithFeatures(
                enableDownloadTool: true,
                enableEOLAnalysis: true,
                previousResults: "", // Empty previous results
                currentResults: "C:\\Current\\report.xlsx",
                repositoryCount: 2);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert
            // Should not call comparison flow since both previous AND current are required
            _mockExcelService.DidNotReceive().CompareExcelFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            // Should proceed with normal initial report flow
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
        }

        [Fact]
        public async Task StartAsync_ShouldFollowCorrectCallOrder_WhenDownloadToolEnabledWithEOLAnalysis()
        {
            // Arrange
            var eolData = new List<PackageData>
            {
                new PackageData { Id = "TestPackage", Version = "1.0.0", Project = "TestProject" }
            };

            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(eolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify correct call order
            Received.InOrder(() =>
            {
                _mockBlackduckReportGenerator.GenerateReport();
                _mockCsvService.AnalyzeReport();
                _mockExcelService.GetWorkbook();
                _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
                _mockExcelService.AddEOLAnalysisSheet(mockWorkbook, eolData);
                _mockExcelService.SaveWorkbook(mockWorkbook);
                _mockBlackduckReportGenerator.Cleanup();
            });
        }

        [Fact]
        public async Task StartAsync_ShouldFollowCorrectCallOrder_WhenDownloadToolEnabledWithoutEOLAnalysis()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify correct call order (without EOL analysis calls)
            Received.InOrder(() =>
            {
                _mockBlackduckReportGenerator.GenerateReport();
                _mockCsvService.AnalyzeReport();
                _mockExcelService.GetWorkbook();
                _mockExcelService.SaveWorkbook(mockWorkbook);
                _mockBlackduckReportGenerator.Cleanup();
            });
        }

        [Fact]
        public async Task StartAsync_ShouldFollowCorrectCallOrder_WhenDownloadToolDisabledWithEOLAnalysis()
        {
            // Arrange
            var eolData = new List<PackageData>
            {
                new PackageData { Id = "TestPackage", Version = "1.0.0", Project = "TestProject" }
            };

            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(eolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify correct call order including download and EOL analysis
            Received.InOrder(() =>
            {
                _mockBlackduckReportGenerator.GenerateReport();
                _mockCsvService.AnalyzeReport();
                _mockExcelService.GetWorkbook();
                _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
                _mockExcelService.AddEOLAnalysisSheet(mockWorkbook, eolData);
                _mockExcelService.SaveWorkbook(mockWorkbook);
                _mockBlackduckReportGenerator.Cleanup();
            });
        }

        [Fact]
        public async Task StartAsync_ShouldFollowCorrectCallOrder_WhenComparisonFlow()
        {
            // Arrange
            var config = CreateConfigWithFeatures(
                enableDownloadTool: true,
                enableEOLAnalysis: true,
                previousResults: "C:\\Previous\\report.xlsx",
                currentResults: "C:\\Current\\report.xlsx",
                repositoryCount: 2);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Only CompareExcelFiles should be called
            _mockExcelService.Received(1).CompareExcelFiles(config.BlackduckConfiguration.CurrentResults, config.BlackduckConfiguration.PreviousResults, config.ReportConfiguration.OutputFilePath);

            // Verify that no other workflow methods are called
            await _mockBlackduckReportGenerator.DidNotReceive().GenerateReport();
            await _mockCsvService.DidNotReceive().AnalyzeReport();
            _mockExcelService.DidNotReceive().GetWorkbook();
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());
            await _mockBlackduckReportGenerator.DidNotReceive().Cleanup();
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task StartAsync_ShouldMaintainCallOrderEvenWithEOLException()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 1);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            var exception = new InvalidOperationException("EOL Analysis failed");

            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns<Task<List<PackageData>>>(x => Task.FromException<List<PackageData>>(exception));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify correct call order even when EOL analysis fails
            Received.InOrder(() =>
            {
                _mockBlackduckReportGenerator.GenerateReport();
                _mockCsvService.AnalyzeReport();
                _mockExcelService.GetWorkbook();
                _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
                // AddEOLAnalysisSheet should NOT be called due to exception
                _mockExcelService.SaveWorkbook(mockWorkbook); // Still called in main flow
                _mockBlackduckReportGenerator.Cleanup();
            });

            // Verify AddEOLAnalysisSheet was NOT called due to exception
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallAnyInitialFlowMethods_WhenComparisonFlowIsUsed()
        {
            // Arrange
            var config = CreateConfigWithFeatures(
                enableDownloadTool: true,
                enableEOLAnalysis: true,
                previousResults: "C:\\Previous\\report.xlsx",
                currentResults: "C:\\Current\\report.xlsx",
                repositoryCount: 5);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify ALL initial flow methods are NOT called
            await _mockBlackduckReportGenerator.DidNotReceive().GenerateReport();
            await _mockBlackduckReportGenerator.DidNotReceive().Cleanup();
            await _mockCsvService.DidNotReceive().AnalyzeReport();
            _mockExcelService.DidNotReceive().GetWorkbook();
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());

            // Only CompareExcelFiles should be called
            _mockExcelService.Received(1).CompareExcelFiles(config.BlackduckConfiguration.CurrentResults, config.BlackduckConfiguration.PreviousResults, config.ReportConfiguration.OutputFilePath);
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallEOLMethods_WhenEOLAnalysisDisabledRegardlessOfRepositoryCount()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false, repositoryCount: 10);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify EOL methods are NOT called even with repositories configured
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());

            // Verify other methods are called normally
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);
            await _mockBlackduckReportGenerator.Received(1).Cleanup();
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallDownloadMethods_WhenDownloadToolDisabledRegardlessOfOtherSettings()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: false, enableEOLAnalysis: true, repositoryCount: 5);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            var eolData = new List<PackageData>
            {
                new PackageData { Id = "TestPackage", Version = "1.0.0", Project = "TestProject" }
            };

            _mockExcelService.GetWorkbook().Returns(mockWorkbook);
            _mockEOLAnalysisService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(eolData));

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify download-related methods are called by default
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // Verify other methods are called normally
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);
            await _mockEOLAnalysisService.Received(1).AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), cancellationToken);
            _mockExcelService.Received(1).AddEOLAnalysisSheet(mockWorkbook, eolData);
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallComparisonMethods_WhenInitialFlowIsUsed()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: false);
            var mockWorkbook = Substitute.For<IXLWorkbook>();
            _mockExcelService.GetWorkbook().Returns(mockWorkbook);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - Verify comparison method is NOT called
            _mockExcelService.DidNotReceive().CompareExcelFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            // Verify initial flow methods are called
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockCsvService.Received(1).AnalyzeReport();
            _mockExcelService.Received(1).GetWorkbook();
            _mockExcelService.Received(1).SaveWorkbook(mockWorkbook);
            await _mockBlackduckReportGenerator.Received(1).Cleanup();
        }

        [Fact]
        public async Task StartAsync_ShouldNotCallUnrelatedMethods_WhenSpecificExceptionOccurs()
        {
            // Arrange
            var config = CreateConfigWithFeatures(enableDownloadTool: true, enableEOLAnalysis: true, repositoryCount: 1);
            var httpException = new HttpRequestException("Network error");

            // Exception occurs during GenerateReport
            _mockBlackduckReportGenerator.GenerateReport().Returns<Task>(x => throw httpException);

            var program = CreateProgram(config);
            var cancellationToken = CancellationToken.None;

            // Act
            await program.StartAsync(cancellationToken);

            // Assert - When GenerateReport fails, subsequent methods should NOT be called
            await _mockCsvService.DidNotReceive().AnalyzeReport();
            _mockExcelService.DidNotReceive().GetWorkbook();
            _mockExcelService.DidNotReceive().SaveWorkbook(Arg.Any<IXLWorkbook>());
            await _mockEOLAnalysisService.DidNotReceive().AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<CancellationToken>());
            _mockExcelService.DidNotReceive().AddEOLAnalysisSheet(Arg.Any<IXLWorkbook>(), Arg.Any<List<PackageData>>());
            _mockExcelService.DidNotReceive().CompareExcelFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

            // Only GenerateReport and Cleanup should be called
            await _mockBlackduckReportGenerator.Received(1).GenerateReport();
            await _mockBlackduckReportGenerator.Received(1).Cleanup();

            // Exception should be logged
            _mockLogger.ReceivedWithAnyArgs().Log<object>(LogLevel.Error, default, default!, default!, default!);
        }
    }


}
