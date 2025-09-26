namespace DART.EOLAnalysis.Models
{
    public class PackageRecommendationConfig
    {
        /// <summary>
        /// Age threshold in years for considering a package old (default: 3.0 years)
        /// </summary>
        public double OldPackageThresholdYears { get; set; } = 3.0;

        /// <summary>
        /// Age threshold in years for considering a package near end-of-life (default: 2.0 years)
        /// </summary>
        public double NearEolThresholdYears { get; set; } = 2.0;

        /// <summary>
        /// Action messages for different scenarios
        /// </summary>
        public PackageActionMessages Messages { get; set; } = new PackageActionMessages();
    }

    public class PackageActionMessages
    {
        /// <summary>
        /// Message for packages over the old threshold with no updates available
        /// </summary>
        public string OldPackageDefault { get; set; } = "Package is over 3 yrs old; investigate or replace/remove.";

        /// <summary>
        /// Message when a newer version is available
        /// </summary>
        public string UpdateToNewer { get; set; } = "Update to newer version";

        /// <summary>
        /// Message for near EOL packages with updates available
        /// </summary>
        public string NearEolUpdate { get; set; } = "Near EOL consider updating to newer version";

        /// <summary>
        /// Message for packages with no action needed
        /// </summary>
        public string NoAction { get; set; } = "N/A";

        /// <summary>
        /// Message when decision cannot be determined
        /// </summary>
        public string ToBeDecided { get; set; } = "TBD";
    }
}