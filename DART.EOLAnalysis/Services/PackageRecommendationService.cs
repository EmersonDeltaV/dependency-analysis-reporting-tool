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

            if (_config == null)
            {
                throw new InvalidOperationException("PackageRecommendationService must be initialized before use. Call Initialize() first.");
            }

            if (package.Age >= _config.OldPackageThresholdYears)
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
            var defaultAction = _config.Messages.OldPackageDefault;

            if (package.LatestVersionDate is not null && package.VersionDate is not null)
            {
                if (DateTime.Parse(package.LatestVersionDate) > DateTime.Parse(package.VersionDate))
                {
                    return _config.Messages.UpdateToNewer;
                }
                else
                {
                    return defaultAction;
                }
            }

            if (package.LatestVersionDate is not null && package.VersionDate is null)
            {
                return _config.Messages.ToBeDecided;
            }

            return defaultAction;
        }

        private string DetermineActionForNearEolPackage(PackageData package)
        {
            if (package.LatestVersionDate is not null && package.VersionDate is not null)
            {
                if (DateTime.Parse(package.LatestVersionDate) > DateTime.Parse(package.VersionDate))
                {
                    return _config.Messages.NearEolUpdate;
                }
            }

            return _config.Messages.NoAction;
        }

        private string DetermineActionForCurrentPackage(PackageData package)
        {
            if (package.LatestVersionDate is not null && package.VersionDate is not null)
            {
                return _config.Messages.NoAction;
            }
            else
            {
                return _config.Messages.ToBeDecided;
            }
        }
    }
}