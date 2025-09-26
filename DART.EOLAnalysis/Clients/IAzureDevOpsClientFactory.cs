namespace DART.EOLAnalysis.Clients
{
    public interface IAzureDevOpsClientFactory
    {
        IAzureDevOpsClient CreateClient(string personalAccessToken);
    }
}