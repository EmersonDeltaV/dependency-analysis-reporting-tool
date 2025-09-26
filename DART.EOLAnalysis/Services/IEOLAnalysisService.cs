using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis
{
    public interface IEOLAnalysisService
    {
        Task<List<PackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config, CancellationToken cancellationToken = default);
    }
}