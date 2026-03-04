using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Analyzes individual .NET project files to extract and evaluate package dependencies.
    /// </summary>
    public interface IProjectAnalysisService
    {
        /// <summary>
        /// Analyzes a project file to extract package references and determine recommended actions.
        /// </summary>
        /// <param name="projectInfo">Information about the project file to analyze.</param>
        /// <param name="recommendationConfig">Configuration for package recommendation logic.</param>
        /// <param name="toggles">Feature toggles (e.g. whether to include dev dependencies).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of package data with metadata and recommendations.</returns>
        Task<List<PackageData>> AnalyzeProjectAsync(
            ProjectInfo projectInfo,
            PackageRecommendationConfig recommendationConfig,
            FeatureToggles toggles,
            CancellationToken cancellationToken = default);
    }
}
