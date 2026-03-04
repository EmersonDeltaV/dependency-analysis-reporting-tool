namespace DART.EOLAnalysis.Models
{
    public class EOLAnalysisConfig
    {
        public string Pat { get; set; } = string.Empty;
        public string NuGetApiUrl { get; set; } = "https://api.nuget.org/v3/index.json";
        public string NpmRegistryUrl { get; set; } = "https://registry.npmjs.org";
        public List<Repository> Repositories { get; set; } = new List<Repository>();
        public PackageRecommendationConfig PackageRecommendation { get; set; } = new PackageRecommendationConfig();
    }

    public class FeatureToggles
    {
        public bool EnableBlackduckAnalysis { get; set; } = true;
        public bool EnableCSharpAnalysis { get; set; } = true;
        public bool EnableNpmAnalysis { get; set; } = true;
        public bool IncludeNpmDevDependencies { get; set; } = false;
    }
}