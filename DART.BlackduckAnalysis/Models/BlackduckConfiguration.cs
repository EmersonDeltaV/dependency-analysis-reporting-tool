namespace DART.BlackduckAnalysis
{
    /// <summary>
    /// Strongly-typed configuration for BlackDuck API integration
    /// </summary>
    public class BlackduckConfiguration
    {
        /// <summary>
        /// BlackDuck API base URL
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// BlackDuck API authentication token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Include transitive dependencies in CSV processing
        /// </summary>
        public bool IncludeTransitiveDependency { get; set; }

        /// <summary>
        /// Comma-separated list of project versions to include
        /// </summary>
        public string ProjectVersionsToInclude { get; set; } = string.Empty;

        /// <summary>
        /// Path to previous results file for comparison
        /// </summary>
        public string PreviousResults { get; set; } = string.Empty;

        /// <summary>
        /// Path to current results file for comparison
        /// </summary>
        public string CurrentResults { get; set; } = string.Empty;

        /// <summary>
        /// Subfolder name for downloaded reports (relative to ReportFolderPath)
        /// </summary>
        public string DownloadedReportsFolderName { get; set; } = "Downloaded";

        /// <summary>
        /// BlackDuck repositories/projects to include in vulnerability reports
        /// </summary>
        public List<BlackduckRepository> BlackduckRepositories { get; set; } = [];

        /// <summary>
        /// Download operation parameters
        /// </summary>
        public DownloadParameters DownloadParameters { get; set; } = new();
    }

    /// <summary>
    /// Represents a BlackDuck repository/project with its API endpoint
    /// </summary>
    public class BlackduckRepository
    {
        /// <summary>
        /// Display name of the repository/project
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// BlackDuck API URL for this repository/project
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parameters for report download operations
    /// </summary>
    public class DownloadParameters
    {
        /// <summary>
        /// Maximum number of polling attempts when waiting for report completion
        /// </summary>
        public int MaxTries { get; set; } = 20;

        /// <summary>
        /// Delay in milliseconds between polling attempts
        /// </summary>
        public int PollingDelayMilliseconds { get; set; } = 5000;
    }
}
