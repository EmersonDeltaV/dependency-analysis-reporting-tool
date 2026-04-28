using DART.Core;
using DART.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DART.Console
{
    public class DartOrchestrator : IHostedService
    {
        private readonly IDartExecutionRunner _executionRunner;
        private readonly ILogger<DartOrchestrator> _logger;
        private readonly Config _config;
        private readonly IHostApplicationLifetime _lifetime;

        private bool IsBlackduckEnabled => _config.FeatureToggles.EnableBlackduckAnalysis;

        public DartOrchestrator(
            IOptions<Config> configOptions,
            IDartExecutionRunner executionRunner,
            IHostApplicationLifetime lifetime,
            ILogger<DartOrchestrator> logger)
        {
            _config = configOptions.Value ?? throw new ConfigException("Failed to load configuration");
            _executionRunner = executionRunner ?? throw new ArgumentNullException(nameof(executionRunner));
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

                var request = ConfigToDartExecutionRequestMapper.Map(_config);
                var progress = new Progress<DartExecutionProgress>(item =>
                    _logger.LogInformation("[{Stage}] {Message}", item.Stage, item.Message));
                var result = await _executionRunner.RunAsync(request, progress, cancellationToken);

                foreach (var issue in result.Issues)
                {
                    _logger.LogWarning("[Runtime] {Source}: {Message}", issue.Source, issue.Message);
                }
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
    }
}
