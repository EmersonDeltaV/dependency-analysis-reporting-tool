namespace DART.EOLAnalysis.Clients
{
    public class AzureDevOpsClientFactory : IAzureDevOpsClientFactory
    {
        public IAzureDevOpsClient CreateClient(string personalAccessToken)
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
            {
                throw new ArgumentException("Personal access token cannot be null or empty.", nameof(personalAccessToken));
            }

            return new AzureDevOpsClient(personalAccessToken);
        }
    }
}