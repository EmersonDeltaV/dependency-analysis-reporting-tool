using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace EOLAnalysisLib
{
    internal static class NugetMetaData
    {
        internal static async Task GetData(CSVHeader data)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                data.Id,
                includePrerelease: true,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            IPackageSearchMetadata currentVersion = packages.FirstOrDefault(p => p.Identity.Version.ToString() == data.Version);
            IPackageSearchMetadata latestVersion = packages.LastOrDefault();

            var currentVersionDate = currentVersion?.Published.GetValueOrDefault().Date;
            var latestVersionDate = latestVersion?.Published.GetValueOrDefault().Date;

            data.VersionDate = currentVersionDate?.ToString("MM/dd/yyyy");
            data.LatestVersion = latestVersion?.Identity.Version.ToString();
            data.LatestVersionDate = latestVersionDate?.ToString("MM/dd/yyyy");
            data.LicenseUrl = currentVersion?.LicenseUrl.ToString();

            data.License = currentVersion?.LicenseMetadata?.License;

            if (currentVersionDate is not null)
            {
                data.Age = Math.Round(((DateTime.Today - currentVersionDate.Value).TotalDays / 365), 1);
            }
            else
            {
                data.Age = 50;
            }

            DecideAction(data);

            //Update to new version
            //Replace the package
            //N/A
        }

        private static void DecideAction(CSVHeader data)
        {
            if (data.Age >= 3)
            {
                var defaultAction = "Package is over 3 yrs old; investigate or replace/remove.";
                if (data.LatestVersionDate is not null && data.VersionDate is not null)
                {
                    if (DateTime.Parse(data.LatestVersionDate) > DateTime.Parse(data.VersionDate))
                    {
                        data.Action = "Update to newer version";
                    }
                    else
                    {
                        data.Action = defaultAction;
                    }
                }
                if (data.LatestVersionDate is not null && data.VersionDate is null)
                {
                    data.Action = "TBD";
                }
                else
                {
                    data.Action = defaultAction;
                }
            }
            else if (data.Age >= 2 && data.Age < 3)
            {
                if (data.LatestVersionDate is not null && data.VersionDate is not null)
                {
                    if (DateTime.Parse(data.LatestVersionDate) > DateTime.Parse(data.VersionDate))
                    {
                        data.Action = "Near EOL consider updating to newer version";
                    }
                }
            }
            else
            {
                if (data.LatestVersionDate is not null && data.VersionDate is not null)
                {
                    data.Action = "N/A";
                }
                else
                {
                    data.Action = "TBD";
                }
            }
        }
    }
}