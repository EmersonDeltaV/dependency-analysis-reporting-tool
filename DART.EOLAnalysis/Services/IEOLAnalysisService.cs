using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis
{
    /// <summary>
    /// Provides End-of-Life (EOL) analysis for .NET package dependencies across multiple repositories.
    /// </summary>
    public interface IEOLAnalysisService
    {
        /// <summary>
        /// Analyzes multiple repositories for package dependencies and their EOL status.
        /// </summary>
        /// <param name="config">Configuration containing repository details and analysis settings.</param>
        /// <param name="cancellationToken">Token to cancel the analysis operation.</param>
        /// <returns>A list of analyzed package data with version information and recommendations.</returns>
        /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when services are not properly initialized.</exception>
        Task<List<PackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config, CancellationToken cancellationToken = default);
    }
}