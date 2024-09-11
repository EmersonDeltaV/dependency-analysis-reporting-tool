namespace BlackduckReportAnalysis.Models
{
    public class Config
    {
        public string ReportFolderPath { get; set; } = string.Empty;
        public string OutputFilePath { get; set; } = string.Empty;
        public bool IncludeTransitiveDependency { get; set; }
        public string Token { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
    }
}
