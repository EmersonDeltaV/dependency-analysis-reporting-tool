namespace DART.Models
{
    /// <summary>
    /// Represents the headers used in the summary report / output file. The order of the headers corresponds to their exact order in the report.
    /// </summary>
    public static class Headers
    {
        public const string Application = "Application";
        public const string SoftwareComponent = "Software Component";
        public const string Version = "Version";
        public const string SecurityRisk = "Security Risk";
        public const string VulnerabilityId = "Vulnerability ID";
        public const string RecommendedFixVersion = "Recommended Fix Version";
        public const string FoundInPreviousScan = "Found in Previous Scan?";
        public const string MatchType = "Match Type";
        public const string ReviewWithCS = "Review with Cybersecurity Team?";
        public const string ActionPlan = "Action Plan";
        public const string FinalStatus = "Final Status / Work item";
        public const string Notes = "Notes";
    }

    /// <summary>
    /// Represents the headers used in the original Blackduck report / csv file. The order of the headers corresponds to their exact order in the report.
    /// </summary>
    public static class BlackduckCSVHeaders
    {
        public const string ProjectName = "Project name";
        public const string ComponentOriginId = "Component origin id";
        public const string SecurityRisk = "Security Risk";
        public const string VulnerabilityId = "Vulnerability ID";
        public const string MatchType = "Match type";
        public const string Version = "Version";
    }

}
