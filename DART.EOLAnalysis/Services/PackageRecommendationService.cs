using DART.EOLAnalysis.Models;

namespace DART.EOLAnalysis.Services
{
    public class PackageRecommendationService : IPackageRecommendationService
    {
        private PackageRecommendationConfig? _config;

        public void Initialize(PackageRecommendationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string DetermineAction(PackageData package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (package.Age >= _config!.OldPackageThresholdYears)
            {
                return DetermineActionForOldPackage(package);
            }
            else if (package.Age >= _config.NearEolThresholdYears && package.Age < _config.OldPackageThresholdYears)
            {
                return DetermineActionForNearEolPackage(package);
            }
            else
            {
                return DetermineActionForCurrentPackage(package);
            }
        }

        private string DetermineActionForOldPackage(PackageData package)
        {
            var defaultAction = _config!.Messages.OldPackageDefault;

            if (!string.IsNullOrEmpty(package.LatestVersionDate) && !string.IsNullOrEmpty(package.VersionDate))
            {
                if (DateTime.Parse(package.LatestVersionDate) > DateTime.Parse(package.VersionDate))
                {
                    return _config!.Messages.UpdateToNewer;
                }
                else
                {
                    return defaultAction;
                }
            }

            if (!string.IsNullOrEmpty(package.LatestVersionDate) && string.IsNullOrEmpty(package.VersionDate))
            {
                return _config!.Messages.ToBeDecided;
            }

            return defaultAction;
        }

        private string DetermineActionForNearEolPackage(PackageData package)
        {
            if (!string.IsNullOrEmpty(package.LatestVersionDate) && !string.IsNullOrEmpty(package.VersionDate))
            {
                if (DateTime.Parse(package.LatestVersionDate) > DateTime.Parse(package.VersionDate))
                {
                    return _config!.Messages.NearEolUpdate;
                }
            }

            return _config!.Messages.NoAction;
        }

        private string DetermineActionForCurrentPackage(PackageData package)
        {
            if (!string.IsNullOrEmpty(package.LatestVersionDate) && !string.IsNullOrEmpty(package.VersionDate))
            {
                return _config!.Messages.NoAction;
            }
            else
            {
                return _config!.Messages.ToBeDecided;
            }
        }
    }
}