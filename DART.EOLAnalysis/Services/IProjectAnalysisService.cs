using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    public interface IProjectAnalysisService
    {
        Task<List<PackageData>> AnalyzeProjectAsync(ProjectInfo projectInfo, PackageRecommendationConfig recommendationConfig, CancellationToken cancellationToken = default);
    }
}