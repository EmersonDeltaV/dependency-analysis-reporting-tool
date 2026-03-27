namespace DART.EOLAnalysis
{
    public class EOLAnalysisConfig
    {
        /// <summary>
        /// GitHub Personal Access Token for accessing repository metadata. Required for fetching latest commit dates to determine EOL status. Should have at least "Repo (read)" scope for public repositories. Not required if all repositories are private and accessible without authentication.
        /// </summary>
        public string Pat { get; set; } = string.Empty;
        public string NuGetApiUrl { get; set; } = "https://api.nuget.org/v3/index.json";
        public string NpmRegistryUrl { get; set; } = "https://registry.npmjs.org";

        /// <summary>
        /// List of repositories to analyze for EOL status. Each repository should specify its type (e.g. NuGet, NPM) and identifier (e.g. package name). Required if EOL analysis is enabled.
        /// </summary>
        public List<Repository> Repositories { get; set; } = new List<Repository>();

        /// <summary>
        /// Configuration for fetching recommended fixes for vulnerable dependencies. This can be used to determine if a vulnerable dependency has a non-vulnerable version available, which may impact EOL status. Required if recommended-fix lookup is enabled by the consuming application. If not needed, this can be left with default values and the service will skip fetching recommended fixes.
        /// </summary>
        public PackageRecommendationConfig PackageRecommendation { get; set; } = new PackageRecommendationConfig();

        /// <summary>
        /// Maximum degree of parallelism for consumers on a bounded channel. Defaults to 10.
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Channel capacity for bounded channel used in I/O operations. This limits the number of items buffered in memory for processing. Defaults to 10.
        /// </summary>
        public int BoundedCapacity { get; set; } = 10;
    }
}
