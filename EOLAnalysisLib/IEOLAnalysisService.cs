using EOLAnalysisLib.Models;

namespace EOLAnalysisLib
{
    public interface IEOLAnalysisService
    {
        Task<List<EOLPackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config);
    }
}