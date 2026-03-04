using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Provides access to npm registry metadata including version history and publication dates.
    /// </summary>
    public interface INpmMetadataService
    {
        /// <summary>
        /// Initializes the service with the specified npm registry URL.
        /// </summary>
        /// <param name="registryUrl">The base URL of the npm registry (e.g., https://registry.npmjs.org).</param>
        /// <exception cref="ArgumentException">Thrown when the registry URL is null or empty.</exception>
        void Initialize(string registryUrl);

        /// <summary>
        /// Retrieves and populates metadata for the specified npm package,
        /// including version dates, latest version, and license information.
        /// </summary>
        /// <param name="data">The package data object to populate with metadata.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <exception cref="InvalidOperationException">Thrown when the service has not been initialized.</exception>
        Task GetDataAsync(PackageData data, CancellationToken cancellationToken = default);
    }
}
