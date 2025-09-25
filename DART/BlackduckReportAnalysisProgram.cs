using BlackduckReportAnalysis;
using BlackduckReportAnalysis.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DART.EOLAnalysis;

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
                                              IConfiguration configuration,
                                              ICsvService csvService,
                                              IExcelService excelService,
                                              IEOLAnalysisService eolAnalysisService,
                                              ILogger<BlackduckReportAnalysisProgram> logger)
        {
            _blackduckReportGenerator = blackduckReportGenerator;
            _config = configuration.Get<Config>() ?? throw new ConfigException("Failed to load configuration");
            _csvService = csvService;
            _excelService = excelService;
            _eolAnalysisService = eolAnalysisService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            try
            {
                if (_config.PreviousResults == string.Empty || _config.CurrentResults == string.Empty)
                {
                    if (_config.FeatureToggles.EnableDownloadTool)
                    {
                        await _blackduckReportGenerator.GenerateReport();
                    }

                    _logger.LogInformation("No previous results found. Skipping comparison.");

                    await _csvService.AnalyzeReport();

                    // Get workbook for potential additional sheets
                    var workbook = _excelService.GetWorkbook();

                    // Add EOL Analysis sheet if enabled and configured
                    if (_config.FeatureToggles.EnableEOLAnalysis &&
                        _config.EOLAnalysis?.Repositories?.Count > 0)
                    {
                        try
                        {
                            _logger.LogInformation("Starting EOL Analysis...");
                            var eolData = await _eolAnalysisService.AnalyzeRepositoriesAsync(_config.EOLAnalysis);

                            if (eolData != null && eolData.Count > 0)
                            {
                                await _excelService.AddEOLAnalysisSheetAsync(workbook, eolData);
                                _logger.LogInformation($"EOL Analysis completed. Found {eolData.Count} packages.");
                            }
                            else
                            {
                                _logger.LogInformation("EOL Analysis completed but no packages were found.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"EOL Analysis failed: {ex.Message}");
                        }
                    }

                    // Save the complete workbook
                    _excelService.SaveWorkbook(workbook);

                    if (_config.FeatureToggles.EnableDownloadTool)
                    {
                        await _blackduckReportGenerator.Cleanup();
                    }
                }
                else
                {
                    _excelService.CompareExcelFiles(_config.CurrentResults, _config.PreviousResults, _config.OutputFilePath);
                }

            }
            catch (HttpRequestException)
            {
                _logger.LogError($"Could not reach {_config.BaseUrl}. Please ensure that you are connected to the corporate VPN.");
            }
            catch (ConfigException ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Encountered an exception: {ex.Message}");
            }

            Console.WriteLine("Press any key to close this window...");
            Console.ReadLine();

        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

    }
}
