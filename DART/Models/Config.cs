using DART.BlackduckAnalysis;
using DART.EOLAnalysis.Models;

namespace DART.Models
{
    public class Config
    {
        public ReportConfiguration ReportConfiguration { get; set; } = new();
        public BlackduckConfiguration BlackduckConfiguration { get; set; } = new();
        public FeatureToggles FeatureToggles { get; set; } = new FeatureToggles();
        public EOLAnalysisConfig EOLAnalysis { get; set; } = new();
    }

    public class ReportConfiguration
    {
        public string OutputFilePath { get; set; } = string.Empty;
        public string LogPath { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string ProductIteration { get; set; } = string.Empty;
    }
    public class FeatureToggles
    {
        public bool EnableBlackduckAnalysis { get; set; } = true;
        public bool EnableEOLAnalysis { get; set; } = true;
    }
}
