using BlackduckReportAnalysis;
using BlackduckReportAnalysis.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DART.EOLAnalysis;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;

namespace BlackduckReportGeneratorTool
{
    public class BlackduckReportAnalysisProgram : IHostedService
    {
        private readonly IBlackduckReportGenerator _blackduckReportGenerator;
        private readonly ICsvService _csvService;
        private readonly IExcelService _excelService;
        private readonly IEOLAnalysisService _eolAnalysisService;
        private readonly ILogger<BlackduckReportAnalysisProgram> _logger;
        private readonly Config _config;

        public BlackduckReportAnalysisProgram(IBlackduckReportGenerator blackduckReportGenerator,
                                              IOptions<Config> configOptions,
                                              ICsvService csvService,
                                              IExcelService excelService,
                                              IEOLAnalysisService eolAnalysisService,
                                              ILogger<BlackduckReportAnalysisProgram> logger)
        {
            _blackduckReportGenerator = blackduckReportGenerator;
            _config = configOptions.Value ?? throw new ConfigException("Failed to load configuration");
            _csvService = csvService;
            _excelService = excelService;
            _eolAnalysisService = eolAnalysisService;
            _logger = logger;

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_config.ReportFolderPath))
                errors.Add("ReportFolderPath is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.OutputFilePath))
                errors.Add("OutputFilePath is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.BaseUrl))
                errors.Add("BaseUrl is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.BlackduckToken))
                errors.Add("BlackduckToken is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.ProductName))
                errors.Add("ProductName is required but not configured");

            if (string.IsNullOrWhiteSpace(_config.ProductVersion))
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
                if (!string.IsNullOrWhiteSpace(_config.PreviousResults) && !string.IsNullOrWhiteSpace(_config.CurrentResults))
                {
                    RunComparisonFlow();
                    return;
                }

                await RunInitialReportFlowAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach {BaseUrl}. Please ensure that you are connected to the corporate VPN. Error: {ErrorMessage}", _config.BaseUrl, ex.Message);
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

            Console.WriteLine("Press any key to close this window...");
            Console.ReadLine();

        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private void RunComparisonFlow()
        {
            _excelService.CompareExcelFiles(_config.CurrentResults, _config.PreviousResults, _config.OutputFilePath);
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
                        _logger.LogInformation($"EOL Analysis completed. Found {eolData.Count} packages.");
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

                // Re-throw to let StartAsync handle the exception
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
