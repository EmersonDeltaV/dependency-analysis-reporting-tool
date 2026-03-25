using DART.BlackduckAnalysis;
using DART.EOLAnalysis;

namespace DART.Console.Models
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
        private string _outputFilePath = string.Empty;
        public string OutputFilePath
        {
            get
            {
                return Path.IsPathRooted(_outputFilePath) ? _outputFilePath : Path.Combine(Directory.GetCurrentDirectory(), _outputFilePath);
            }
            set => _outputFilePath = value;
        }
        private string _logPath = string.Empty;
        public string LogPath
        {
            get
            {
                return Path.IsPathRooted(_logPath) ? _logPath : Path.Combine(Directory.GetCurrentDirectory(), _logPath);
            }
            set => _logPath = value;
        }
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string ProductIteration { get; set; } = string.Empty;
    }
}

