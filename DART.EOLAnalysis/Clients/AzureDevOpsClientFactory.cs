using Microsoft.Extensions.Logging;

namespace DART.EOLAnalysis.Clients
{
    public class AzureDevOpsClientFactory : IAzureDevOpsClientFactory
    {
        private readonly ILogger<AzureDevOpsClient> _logger;

        public AzureDevOpsClientFactory(ILogger<AzureDevOpsClient> logger)
        {
            _logger = logger;
        }

        public IAzureDevOpsClient CreateClient(string personalAccessToken)
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                throw new ArgumentException("Personal access token cannot be null or empty.", nameof(personalAccessToken));
            }

            return new AzureDevOpsClient(personalAccessToken, _logger);
        }
    }
}