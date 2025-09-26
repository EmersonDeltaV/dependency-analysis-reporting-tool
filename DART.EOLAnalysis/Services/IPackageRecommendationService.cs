using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    public interface IPackageRecommendationService
    {
        void Initialize(PackageRecommendationConfig config);
        string DetermineAction(PackageData package);
    }
}