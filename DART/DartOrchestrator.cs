using DART.Core;
using CoreRowDetails = DART.Core.RowDetails;
using DART.EOLAnalysis;
using DART.Exceptions;
using DART.ReportGenerator;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DART
{
    public class DartOrchestrator : IHostedService
    {
        private readonly IAnalysisOrchestrator _coreAnalysisOrchestrator;
        private readonly IReportGenerator _reportGenerator;
        private readonly ILogger<DartOrchestrator> _logger;
        private readonly Config _config;
        private readonly IHostApplicationLifetime _lifetime;

        private bool IsBlackduckEnabled => _config.FeatureToggles.EnableBlackduckAnalysis;
        private bool HasBothBlackduckResults =>
            !string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.PreviousResults) &&
            !string.IsNullOrWhiteSpace(_config.BlackduckConfiguration.CurrentResults);

        public DartOrchestrator(
            IOptions<Config> configOptions,
            IAnalysisOrchestrator coreAnalysisOrchestrator,
            IReportGenerator reportGenerator,
            IHostApplicationLifetime lifetime,
            ILogger<DartOrchestrator> logger)
        {
            _config = configOptions.Value ?? throw new ConfigException("Failed to load configuration");
            _coreAnalysisOrchestrator = coreAnalysisOrchestrator ?? throw new ArgumentNullException(nameof(coreAnalysisOrchestrator));
            _reportGenerator = reportGenerator ?? throw new ArgumentNullException(nameof(reportGenerator));
            _lifetime = lifetime;
            _logger = logger;

            _config.FeatureToggles ??= new FeatureToggles();

            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_config.ReportConfiguration.OutputFilePath))
                errors.Add("OutputFilePath is required but not configured");

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

                if (HasBothBlackduckResults)
                {
                    _reportGenerator.CompareCurrentWithPrevious(
                        _config.BlackduckConfiguration.CurrentResults,
                        _config.BlackduckConfiguration.PreviousResults);
                    return;
                }

                _logger.LogInformation("No previous results found. Skipping comparison.");
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

            var result = await _coreAnalysisOrchestrator.RunAsync(request, cancellationToken);

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
            var reportPath = result.EolFindings.Count == 0
                ? _reportGenerator.GenerateCurrentFormatReport(
                    rows,
                    _config.ReportConfiguration.OutputFilePath,
                    appCode,
                    _config.ReportConfiguration.ProductName,
                    _config.ReportConfiguration.ProductVersion,
                    _config.ReportConfiguration.ProductIteration)
                : _reportGenerator.GenerateCurrentFormatReport(
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
    }
}
