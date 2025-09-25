namespace BlackduckReportAnalysis.Models
{
    public class Config
    {
        public string ReportFolderPath { get; set; } = string.Empty;
        public string OutputFilePath { get; set; } = string.Empty;
        public bool IncludeTransitiveDependency { get; set; }
        public string BlackduckToken { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
        public string ProjectVersionsToInclude { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string ProductIteration { get; set; } = string.Empty;
        public string PreviousResults { get; set; } = string.Empty;
        public string CurrentResults { get; set; } = string.Empty;
        public FeatureToggles FeatureToggles { get; set; } = new FeatureToggles();
        public EOLAnalysisConfig EOLAnalysis { get; set; } = new();
    }
}
