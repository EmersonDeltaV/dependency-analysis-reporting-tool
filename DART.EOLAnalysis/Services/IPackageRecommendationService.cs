using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Provides package recommendation logic based on configurable age thresholds and business rules.
    /// </summary>
    public interface IPackageRecommendationService
    {
        /// <summary>
        /// Initializes the service with the specified recommendation configuration.
        /// </summary>
        /// <param name="config">Configuration containing age thresholds and recommendation messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when the configuration is null.</exception>
        void Initialize(PackageRecommendationConfig config);

        /// <summary>
        /// Determines the recommended action for a package based on its age and other factors.
        /// </summary>
        /// <param name="package">The package data to evaluate.</param>
        /// <returns>A recommendation message indicating the suggested action for the package.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the package is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the service has not been initialized.</exception>
        string DetermineAction(PackageData package);
    }
}