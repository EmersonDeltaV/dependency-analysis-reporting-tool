using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Provides access to NuGet package metadata including version history and publication dates.
    /// </summary>
    public interface INugetMetadataService
    {
        /// <summary>
        /// Initializes the service with the specified NuGet API endpoint.
        /// </summary>
        /// <param name="nugetApiUrl">The URL of the NuGet API to use for metadata retrieval.</param>
        /// <exception cref="ArgumentException">Thrown when the NuGet API URL is null or empty.</exception>
        void Initialize(string nugetApiUrl);

        /// <summary>
        /// Retrieves and populates metadata for the specified package, including version dates and licensing information.
        /// </summary>
        /// <param name="data">The package data object to populate with metadata.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown when the service has not been initialized.</exception>
        Task GetDataAsync(PackageData data, CancellationToken cancellationToken = default);
    }
}