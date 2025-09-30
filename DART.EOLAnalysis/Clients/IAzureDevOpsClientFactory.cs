namespace DART.EOLAnalysis.Clients
{
    /// <summary>
    /// Factory for creating Azure DevOps client instances with authentication.
    /// </summary>
    public interface IAzureDevOpsClientFactory
    {
        /// <summary>
        /// Creates a new Azure DevOps client configured with the specified personal access token.
        /// </summary>
        /// <param name="personalAccessToken">The personal access token for Azure DevOps authentication.</param>
        /// <returns>A configured Azure DevOps client instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the personal access token is null or empty.</exception>
        IAzureDevOpsClient CreateClient(string personalAccessToken);
    }
}