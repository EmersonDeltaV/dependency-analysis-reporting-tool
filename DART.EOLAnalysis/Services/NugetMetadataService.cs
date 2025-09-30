using DART.EOLAnalysis.Models;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace DART.EOLAnalysis.Services
{
    public class NugetMetadataService : INugetMetadataService
    {
        private SourceRepository? _repository;

        public void Initialize(string nugetApiUrl)
        {
            if (string.IsNullOrWhiteSpace(nugetApiUrl))
            {
                throw new ArgumentException("NuGet API URL cannot be null or empty.", nameof(nugetApiUrl));
            }

            _repository = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3(nugetApiUrl);
        }

        public async Task GetDataAsync(PackageData data, CancellationToken cancellationToken = default)
        {
            if (_repository == null)
            {
                throw new InvalidOperationException("NugetMetadataService must be initialized before use. Call Initialize() first.");
            }

            ILogger logger = NullLogger.Instance;

            SourceCacheContext cache = new SourceCacheContext();
            PackageMetadataResource resource = await _repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

            IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                data.Id,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            IPackageSearchMetadata? currentVersion = packages.FirstOrDefault(p => p.Identity.Version.ToString() == data.Version);
            IPackageSearchMetadata? latestVersion = packages.LastOrDefault();

            var currentVersionDate = currentVersion?.Published.GetValueOrDefault().Date;
            var latestVersionDate = latestVersion?.Published.GetValueOrDefault().Date;

            data.VersionDate = currentVersionDate?.ToString("MM/dd/yyyy") ?? string.Empty;
            data.LatestVersion = latestVersion?.Identity.Version.ToString() ?? string.Empty;
            data.LatestVersionDate = latestVersionDate?.ToString("MM/dd/yyyy") ?? string.Empty;
            data.LicenseUrl = currentVersion?.LicenseUrl?.ToString() ?? string.Empty;

            data.License = currentVersion?.LicenseMetadata?.License ?? string.Empty;

            if (currentVersionDate is not null)
            {
                data.Age = Math.Round((DateTime.Today - currentVersionDate.Value).TotalDays / 365, 1);
            }
            else
            {
                data.Age = 50;
            }

            //Update to new version
            //Replace the package
            //N/A
        }
    }
}