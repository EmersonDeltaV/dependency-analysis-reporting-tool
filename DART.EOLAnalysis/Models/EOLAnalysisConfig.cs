namespace DART.EOLAnalysis.Models
{
    public class EOLAnalysisConfig
    {
        public string Pat { get; set; } = string.Empty;
        public List<Repository> Repositories { get; set; } = new List<Repository>();
    }
}