namespace DART.EOLAnalysis
{
    /// <summary>
    /// Provides End-of-Life (EOL) analysis for package dependencies across multiple repositories.
    /// Supports C# (.csproj / NuGet) and npm (package.json) ecosystems, controlled via <see cref="FeatureToggles"/>.
    /// </summary>
    public interface IEOLAnalysisService
    {
        /// <summary>
        /// Analyzes multiple repositories for package dependencies and their EOL status.
        /// </summary>
        /// <param name="config">Configuration containing repository details and analysis settings.</param>
        /// <param name="toggles">Feature toggles controlling which ecosystems (C#, npm) to analyse.</param>
        /// <param name="cancellationToken">Token to cancel the analysis operation.</param>
        /// <returns>A list of analyzed package data with version information and recommendations.</returns>
        Task<List<PackageData>> AnalyzeRepositoriesAsync(EOLAnalysisConfig config, FeatureToggles toggles, CancellationToken cancellationToken = default);
    }
}