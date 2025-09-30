namespace DART.EOLAnalysis.Models
{
    public class EOLAnalysisConfig
    {
        public string Pat { get; set; } = string.Empty;
        public string NuGetApiUrl { get; set; } = "https://api.nuget.org/v3/index.json";
        public List<Repository> Repositories { get; set; } = new List<Repository>();
        public PackageRecommendationConfig PackageRecommendation { get; set; } = new PackageRecommendationConfig();
    }
}