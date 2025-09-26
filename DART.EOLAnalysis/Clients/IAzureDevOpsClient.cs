using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Clients
{
    public interface IAzureDevOpsClient : IDisposable
    {
        Task<List<GitItem>> FindCsProjFilesAsync(Repository repository, CancellationToken cancellationToken = default);
        Task<string> GetFileContentAsync(Repository repository, string filePath, CancellationToken cancellationToken = default);
    }
}