using ClosedXML.Excel;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using DART.Exceptions;
using DART.Models;
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
        private readonly ILogger<DartOrchestrator> _logger;
        private readonly Config _config;

        public DartOrchestrator(IBlackduckReportGenerator blackduckReportGenerator,
                                IOptions<Config> configOptions,
                                ICsvService csvService,
                                IExcelService excelService,
                                IEOLAnalysisService eolAnalysisService,
                                ILogger<DartOrchestrator> logger)
        {
            _blackduckReportGenerator = blackduckReportGenerator;
            _config = configOptions.Value ?? throw new ConfigException("Failed to load configuration");
            _csvService = csvService;
            _excelService = excelService;
            _eolAnalysisService = eolAnalysisService;
            _logger = logger;

            // Ensure feature toggles are non-null to simplify checks
            _config.FeatureToggles ??= new FeatureToggles();

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.ReportFolderPath))
                errors.Add("ReportFolderPath is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.OutputFilePath))
                errors.Add("OutputFilePath is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.BaseUrl))
                errors.Add("BlackduckConfiguration:BaseUrl is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.Token))
                errors.Add("BlackduckConfiguration:Token is required but not configured");

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

                if (!string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.PreviousResults) && !string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.CurrentResults))
                {
                    RunComparisonFlow();
                    return;
                }

                await RunInitialReportFlowAsync(cancellationToken);
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
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private void RunComparisonFlow()
        {
            _excelService.CompareExcelFiles(_config.BlackduckConfiguration.CurrentResults, _config.BlackduckConfiguration.PreviousResults, _config.ReportConfiguration.OutputFilePath);
        }

        private async Task RunEolAnalysisAsync(IXLWorkbook workbook, CancellationToken cancellationToken)
        {
            if (_config.FeatureToggles.EnableEOLAnalysis &&
                _config.EOLAnalysis?.Repositories?.Count > 0)
            {
                try
                {
                    _logger.LogInformation("Starting EOL Analysis...");
                    var eolData = await _eolAnalysisService.AnalyzeRepositoriesAsync(_config.EOLAnalysis, cancellationToken);

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
        }

        private async Task RunInitialReportFlowAsync(CancellationToken cancellationToken)
        {
            IXLWorkbook? workbook = null;

            try
            {
                if (_config.FeatureToggles.EnableDownloadTool)
                {
                    _blackduckReportGenerator.SetRuntimeConfig(_config.BlackduckConfiguration, _config.ReportConfiguration.ReportFolderPath);
                    await _blackduckReportGenerator.GenerateReport();
                }

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

                // Always cleanup if download tool was enabled
                if (_config.FeatureToggles.EnableDownloadTool)
                {
                    await _blackduckReportGenerator.Cleanup();
                }

                throw;
            }

            // Save the complete workbook on success
            if (workbook != null)
            {
                _excelService.SaveWorkbook(workbook);
            }

            if (_config.FeatureToggles.EnableDownloadTool)
            {
                await _blackduckReportGenerator.Cleanup();
            }
        }

    }
}
