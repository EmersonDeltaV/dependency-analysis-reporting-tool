using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Clients;

namespace DART.EOLAnalysis.Services
{
    public interface IRepositoryProcessorService
    {
        Task<List<ProjectInfo>> ProcessRepositoryAsync(Repository repository, IAzureDevOpsClient azureDevOpsClient, CancellationToken cancellationToken = default);
    }
}