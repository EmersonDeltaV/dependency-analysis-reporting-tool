namespace BlackduckReportAnalysis.Models
{
    public class EOLAnalysisConfig
    {
        public string Pat { get; set; } = string.Empty;
        public List<Repository> Repositories { get; set; } = new List<Repository>();
    }

    public class Repository
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}