namespace DART.EOLAnalysis.Models
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
        /// Configuration for fetching recommended fixes for vulnerable dependencies. This can be used to determine if a vulnerable dependency has a non-vulnerable version available, which may impact EOL status. Required if IncludeRecommendedFix is true in BlackduckConfiguration. If not needed, this can be left with default values and the service will skip fetching recommended fixes.
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

    public class FeatureToggles
    {
        /// <summary>
        /// When true, runs all Black Duck download, processing, and comparison steps. When false, Black Duck steps are skipped. Defaults to true. Black Duck configuration is only required when this is true.
        /// </summary>
        public bool EnableBlackduckAnalysis { get; set; } = true;

        /// <summary>
        /// When true, adds an EOL analysis sheet for CSharp projects. Can run standalone (with Black Duck disabled) or alongside Black Duck. Requires EOL repo configuration.
        /// </summary>
        public bool EnableCSharpAnalysis { get; set; } = true;

        /// <summary>
        /// When true, adds an EOL analysis sheet for NPM projects. Can run standalone (with Black Duck disabled) or alongside Black Duck. Requires EOL repo configuration.
        /// </summary>
        public bool EnableNpmAnalysis { get; set; } = true;

        /// <summary>
        /// When true, includes dev dependencies in NPM EOL analysis. Defaults to false.
        /// </summary>
        public bool IncludeNpmDevDependencies { get; set; } = false;
    }
}