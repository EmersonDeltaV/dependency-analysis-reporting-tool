using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Clients
{
    /// <summary>
    /// Provides access to Azure DevOps repositories for retrieving project files and content.
    /// </summary>
    public interface IAzureDevOpsClient : IDisposable
    {
        /// <summary>
        /// Finds all .csproj files in the specified repository.
        /// </summary>
        /// <param name="repository">The repository to search for project files.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of Git items representing .csproj files found in the repository.</returns>
        Task<List<GitItem>> FindCsProjFilesAsync(Repository repository, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the content of a specific file from the repository.
        /// </summary>
        /// <param name="repository">The repository containing the file.</param>
        /// <param name="filePath">The path to the file within the repository.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The string content of the specified file.</returns>
        Task<string> GetFileContentAsync(Repository repository, string filePath, CancellationToken cancellationToken = default);
    }
}