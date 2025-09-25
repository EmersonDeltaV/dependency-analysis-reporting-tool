namespace EOLAnalysisLib
{
    internal class CSVHeader
    {
        public string Id { get; set; }
        public string Project { get; set; }
        public string Version { get; set; }
        public string VersionDate { get; set; }
        public double Age { get; set; }
        public string LatestVersion { get; set; }
        public string LatestVersionDate { get; set; }
        public string License { get; set; }
        public string LicenseUrl { get; set; }
        public string Action { get; set; }
    }
}