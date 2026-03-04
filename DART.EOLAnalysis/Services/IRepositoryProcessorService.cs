using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Clients;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Processes Azure DevOps repositories to discover and retrieve project files (.csproj and/or package.json).
    /// </summary>
    public interface IRepositoryProcessorService
    {
        /// <summary>
        /// Processes a repository to find all relevant project files based on the enabled ecosystems,
        /// and retrieves their content. Returns csproj entries first, then package.json entries.
        /// </summary>
        /// <param name="repository">The repository to process.</param>
        /// <param name="azureDevOpsClient">The Azure DevOps client for repository access.</param>
        /// <param name="toggles">Feature toggles controlling which ecosystems to discover.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of project information including file paths, content, and project type.</returns>
        Task<List<ProjectInfo>> ProcessRepositoryAsync(Repository repository, IAzureDevOpsClient azureDevOpsClient, FeatureToggles toggles, CancellationToken cancellationToken = default);
    }
}