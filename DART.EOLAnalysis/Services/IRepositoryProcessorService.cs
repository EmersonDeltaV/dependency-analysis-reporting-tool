using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Clients;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Processes Azure DevOps repositories to discover and retrieve .NET project files.
    /// </summary>
    public interface IRepositoryProcessorService
    {
        /// <summary>
        /// Processes a repository to find all .csproj files and retrieve their content.
        /// </summary>
        /// <param name="repository">The repository to process.</param>
        /// <param name="azureDevOpsClient">The Azure DevOps client for repository access.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of project information including file paths and content.</returns>
        Task<List<ProjectInfo>> ProcessRepositoryAsync(Repository repository, IAzureDevOpsClient azureDevOpsClient, CancellationToken cancellationToken = default);
    }
}