using ClosedXML.Excel;
using DART.BlackduckAnalysis;
using DART.Core.Contracts;
using DART.Core.Services;
using CoreRowDetails = DART.Core.Models.RowDetails;
using DART.EOLAnalysis;
using DART.EOLAnalysis.Models;
using DART.Exceptions;
using DART.Models;
using DART.ReportGenerator.Interfaces;
using DART.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DART
{
    public class DartOrchestrator : IHostedService
    {
        private readonly IBlackduckReportGenerator _blackduckReportGenerator;
        private readonly ICsvService _csvService;
        private readonly IExcelService _excelService;
        private readonly IEOLAnalysisService _eolAnalysisService;
        private readonly IAnalysisOrchestrator? _coreAnalysisOrchestrator;
        private readonly IReportGenerator? _reportGenerator;
        private readonly ILogger<DartOrchestrator> _logger;
        private readonly Config _config;
        private readonly IHostApplicationLifetime _lifetime;

        private bool IsBlackduckEnabled => _config.FeatureToggles.EnableBlackduckAnalysis;
        private bool IsAnyEolEnabledAndConfigured =>
            (_config.FeatureToggles.EnableCSharpAnalysis || _config.FeatureToggles.EnableNpmAnalysis)
            && (_config.EOLAnalysis?.Repositories?.Count > 0);
        private bool HasBothBlackduckResults =>
            !string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.PreviousResults) &&
            !string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.CurrentResults);

        public DartOrchestrator(IBlackduckReportGenerator blackduckReportGenerator,
                                IOptions<Config> configOptions,
                                ICsvService csvService,
                                IExcelService excelService,
                                IEOLAnalysisService eolAnalysisService,
                                IHostApplicationLifetime lifetime,
                                ILogger<DartOrchestrator> logger,
                                IAnalysisOrchestrator? coreAnalysisOrchestrator = null,
                                IReportGenerator? reportGenerator = null)
        {
            _blackduckReportGenerator = blackduckReportGenerator;
            _config = configOptions.Value ?? throw new ConfigException("Failed to load configuration");
            _csvService = csvService;
            _excelService = excelService;
            _eolAnalysisService = eolAnalysisService;
            _lifetime = lifetime;
            _logger = logger;
            _coreAnalysisOrchestrator = coreAnalysisOrchestrator;
            _reportGenerator = reportGenerator;

            // Ensure feature toggles are non-null to simplify checks
            _config.FeatureToggles ??= new FeatureToggles();

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.OutputFilePath))
                errors.Add("OutputFilePath is required but not configured");

            // Black Duck settings are required only when the feature is enabled
            if (IsBlackduckEnabled)
            {
                if (string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.BaseUrl))
                    errors.Add("BlackduckConfiguration:BaseUrl is required but not configured");

                if (string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.Token))
                    errors.Add("BlackduckConfiguration:Token is required but not configured");
            }

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.ProductName))
                errors.Add("ProductName is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.ProductVersion))
                errors.Add("ProductVersion is required but not configured");

            if (errors.Count > 0)
            {
                var errorMessage = $"Configuration validation failed: {string.Join("; ", errors)}";
                throw new ConfigException(errorMessage);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting analysis...");

                if (_coreAnalysisOrchestrator is null || _reportGenerator is null)
                {
                    await RunLegacyFlowAsync(cancellationToken);
                    return;
                }

                if (HasBothBlackduckResults)
                {
                    _reportGenerator.CompareCurrentWithPrevious(
                        _config.BlackduckConfiguration.CurrentResults,
                        _config.BlackduckConfiguration.PreviousResults);
                    return;
                }

                await RunCoreOrchestrationFlowAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach {BaseUrl}. Please ensure that you are connected to the corporate VPN. Error: {ErrorMessage}", _config.BlackduckConfiguration.BaseUrl, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Configuration Error: {ErrorMessage}", ex.Message);
            }
            catch (ConfigException ex)
            {
                _logger.LogError(ex, "ERROR: {ErrorMessage}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encountered an exception: {ErrorMessage}", ex.Message);
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private async Task RunLegacyFlowAsync(CancellationToken cancellationToken)
        {
            if (!IsBlackduckEnabled)
            {
                _logger.LogInformation("Black Duck analysis disabled by feature toggle; skipping Black Duck steps.");
                await RunEolOnlyFlowAsync(cancellationToken);
                return;
            }

            if (HasBothBlackduckResults)
            {
                RunComparisonFlow();
                return;
            }

            await RunInitialReportFlowAsync(cancellationToken);

            if (HasBothBlackduckResults)
            {
                RunComparisonFlow();
            }
        }

        private async Task RunCoreOrchestrationFlowAsync(CancellationToken cancellationToken)
        {
            var enableEolAnalysis =
                _config.FeatureToggles.EnableCSharpAnalysis ||
                _config.FeatureToggles.EnableNpmAnalysis;

            var request = new AnalysisRequest
            {
                EnableBlackduckAnalysis = _config.FeatureToggles.EnableBlackduckAnalysis,
                EnableEolAnalysis = enableEolAnalysis
            };

            var result = await _coreAnalysisOrchestrator!.RunAsync(request, cancellationToken);

            foreach (var issue in result.Issues)
            {
                _logger.LogWarning("[CoreAnalysis] {Source}: {Message}", issue.Source, issue.Message);
            }

            if (!request.EnableBlackduckAnalysis && !request.EnableEolAnalysis)
            {
                return;
            }

            var rows = result.BlackduckFindings.Select(finding => new CoreRowDetails
            {
                ApplicationName = finding.ApplicationName,
                SoftwareComponent = finding.SoftwareComponent,
                Version = finding.Version,
                SecurityRisk = finding.SecurityRisk,
                VulnerabilityId = finding.VulnerabilityId,
                RecommendedFix = finding.RecommendedFix,
                MatchType = finding.MatchType
            }).ToList();

            var appCode = Environment.GetEnvironmentVariable("DART_APP_CODE") ?? "app";
            var reportPath = _reportGenerator!.GenerateCurrentFormatReport(
                rows,
                result.EolFindings,
                _config.ReportConfiguration.OutputFilePath,
                appCode,
                _config.ReportConfiguration.ProductName,
                _config.ReportConfiguration.ProductVersion,
                _config.ReportConfiguration.ProductIteration);

            if (request.EnableBlackduckAnalysis && string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.CurrentResults))
            {
                _config.BlackduckConfiguration.CurrentResults = reportPath;
            }
        }

        private void RunComparisonFlow()
        {
            _excelService.CompareExcelFiles(
                _config.BlackduckConfiguration.CurrentResults,
                _config.BlackduckConfiguration.PreviousResults,
                _config.ReportConfiguration.OutputFilePath);
        }

        private async Task RunEolOnlyFlowAsync(CancellationToken cancellationToken)
        {
            if (!IsAnyEolEnabledAndConfigured)
                return;

            var workbook = _excelService.GetWorkbook();
            await RunEolAnalysisAsync(workbook, cancellationToken);
            _excelService.SaveWorkbook(workbook);
        }

        private async Task RunEolAnalysisAsync(IXLWorkbook workbook, CancellationToken cancellationToken)
        {
            if (!IsAnyEolEnabledAndConfigured)
                return;

            try
            {
                _logger.LogInformation("Starting EOL Analysis...");

                var eolData = await _eolAnalysisService.AnalyzeRepositoriesAsync(_config.EOLAnalysis, _config.FeatureToggles, cancellationToken);

                if (eolData != null && eolData.Count > 0)
                {
                    _excelService.AddEOLAnalysisSheet(workbook, eolData);
                    _logger.LogInformation("EOL Analysis completed. Found {PackageCount} packages.", eolData.Count);
                }
                else
                {
                    _logger.LogInformation("EOL Analysis completed but no packages were found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EOL Analysis failed: {ErrorMessage}", ex.Message);
            }
        }

        private async Task RunInitialReportFlowAsync(CancellationToken cancellationToken)
        {
            IXLWorkbook? workbook = null;

            try
            {
                _blackduckReportGenerator.SetRuntimeConfig(_config.BlackduckConfiguration, _config.ReportConfiguration.OutputFilePath);
                await _blackduckReportGenerator.GenerateReport();

                _logger.LogInformation("No previous results found. Skipping comparison.");

                await _csvService.AnalyzeReport();

                // Get workbook for potential additional sheets
                workbook = _excelService.GetWorkbook();

                // Add EOL Analysis sheet if enabled and configured
                await RunEolAnalysisAsync(workbook, cancellationToken);
            }
            catch
            {
                // Save workbook if it was created, even on exception
                if (workbook != null)
                {
                    _excelService.SaveWorkbook(workbook);
                }

                await _blackduckReportGenerator.Cleanup();

                throw;
            }

            // Save the complete workbook on success
            if (workbook != null)
            {
                _excelService.SaveWorkbook(workbook);
            }

            await _blackduckReportGenerator.Cleanup();

            SetCurrentResultsFromOutputDirectoryIfMissing();
        }

        private void SetCurrentResultsFromOutputDirectoryIfMissing()
        {
            if (!string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.CurrentResults))
                return;

            try
            {
                var outputDir = _config.ReportConfiguration.OutputFilePath;
                if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
                    return;

                var latestSummary = Directory
                    .GetFiles(outputDir, "dart-summary-*.xlsx")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latestSummary != null)
                {
                    _config.BlackduckConfiguration.CurrentResults = latestSummary.FullName;
                    _logger.LogInformation("CurrentResults not provided; using generated summary at {FilePath}.", latestSummary.FullName);
                }
                else
                {
                    _logger.LogWarning("CurrentResults not provided and no generated summary found in {OutputDir}.", outputDir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set CurrentResults from generated output.");
            }
        }
    }
}
