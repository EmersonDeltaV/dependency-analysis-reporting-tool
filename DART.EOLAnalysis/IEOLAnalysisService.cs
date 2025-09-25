using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis
{
    public interface IEOLAnalysisService
    {
        Task<List<EOLPackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config);
    }
}