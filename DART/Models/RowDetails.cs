namespace DART.Models
{
    public class RowDetails
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string SoftwareComponent { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string SecurityRisk { get; set; } = string.Empty;
        public string VulnerabilityId { get; set; } = string.Empty;
        public string RecommendedFix { get; set; } = string.Empty;
        public string MatchType { get; set; } = string.Empty;
    }
}