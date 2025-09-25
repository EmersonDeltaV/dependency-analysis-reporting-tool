namespace DART.EOLAnalysis
{
    public class AppConfiguration
    {
        public string OutputPath { get; set; } = string.Empty;
        public string Pat { get; set; } = string.Empty;
        public List<Repository> Repositories { get; set; } = new List<Repository>();
    }
}