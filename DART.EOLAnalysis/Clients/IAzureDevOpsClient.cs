using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Clients
{
    public interface IAzureDevOpsClient
    {
        Task<List<GitItem>> FindCsProjFilesAsync(Repository repository);
        Task<string> GetFileContentAsync(Repository repository, string filePath);
    }
}