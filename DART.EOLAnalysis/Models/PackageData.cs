namespace DART.EOLAnalysis.Models
{
    public class PackageData
    {
        public string Id { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string VersionDate { get; set; } = string.Empty;
        public double Age { get; set; }
        public string LatestVersion { get; set; } = string.Empty;
        public string LatestVersionDate { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string LicenseUrl { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}