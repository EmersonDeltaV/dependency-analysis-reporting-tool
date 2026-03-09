using DART.EOLAnalysis.Helpers;
using DART.EOLAnalysis.Models;
using System.Text.Json;

namespace DART.EOLAnalysis.Services
{
    /// <summary>
    /// Retrieves npm package metadata from the npm registry REST API.
    /// API: GET {registryUrl}/{packageName}
    /// </summary>
    public class NpmMetadataService : INpmMetadataService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private string? _registryUrl;

        public void Initialize(string registryUrl)
        {
            if (string.IsNullOrWhiteSpace(registryUrl))
            {
                throw new ArgumentException("npm registry URL cannot be null or empty.", nameof(registryUrl));
            }

            _registryUrl = registryUrl.TrimEnd('/');
        }

        public async Task GetDataAsync(PackageData data, CancellationToken cancellationToken = default)
        {
            if (_registryUrl == null)
            {
                throw new InvalidOperationException("NpmMetadataService must be initialized before use. Call Initialize() first.");
            }

            // npm registry API: GET https://registry.npmjs.org/<package>
            var url = $"{_registryUrl}/{Uri.EscapeDataString(data.Id)}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Package not found or registry unavailable — leave fields empty
                return;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Resolve the version to query (strip semver range prefix e.g. ^, ~, >=)
            var resolvedVersion = PackageJsonHelper.StripSemverPrefix(data.Version);

            // "time" object: { "1.0.0": "2020-01-15T10:00:00.000Z", ... }
            DateTime? currentVersionDate = null;
            DateTime? latestVersionDate = null;
            string latestVersion = string.Empty;

            if (root.TryGetProperty("time", out var timeObj))
            {
                if (timeObj.TryGetProperty(resolvedVersion, out var publishedEl)
                    && DateTime.TryParse(publishedEl.GetString(), out var parsedDate))
                {
                    currentVersionDate = parsedDate.Date;
                }
            }

            // "dist-tags": { "latest": "x.y.z" }
            if (root.TryGetProperty("dist-tags", out var distTags)
                && distTags.TryGetProperty("latest", out var latestEl))
            {
                latestVersion = latestEl.GetString() ?? string.Empty;

                if (!string.IsNullOrEmpty(latestVersion)
                    && root.TryGetProperty("time", out var timeObj2)
                    && timeObj2.TryGetProperty(latestVersion, out var latestDateEl)
                    && DateTime.TryParse(latestDateEl.GetString(), out var parsedLatest))
                {
                    latestVersionDate = parsedLatest.Date;
                }
            }

            // License: check top-level "license" field first, then versions[x].license
            string license = string.Empty;
            if (root.TryGetProperty("license", out var licenseEl))
            {
                license = licenseEl.ValueKind == JsonValueKind.String
                    ? licenseEl.GetString() ?? string.Empty
                    : string.Empty;
            }

            if (string.IsNullOrEmpty(license)
                && root.TryGetProperty("versions", out var versions)
                && versions.TryGetProperty(resolvedVersion, out var versionObj)
                && versionObj.TryGetProperty("license", out var versionLicenseEl))
            {
                license = versionLicenseEl.ValueKind == JsonValueKind.String
                    ? versionLicenseEl.GetString() ?? string.Empty
                    : string.Empty;
            }

            // npm registry URL for the package (used as LicenseUrl substitute)
            var registryPackageUrl = $"https://www.npmjs.com/package/{data.Id}";

            // Populate PackageData
            data.VersionDate = currentVersionDate?.ToString("MM/dd/yyyy") ?? string.Empty;
            data.LatestVersion = latestVersion;
            data.LatestVersionDate = latestVersionDate?.ToString("MM/dd/yyyy") ?? string.Empty;
            data.License = license;
            data.LicenseUrl = registryPackageUrl;

            if (currentVersionDate is not null)
            {
                data.Age = Math.Round((DateTime.Today - currentVersionDate.Value).TotalDays / 365, 1);
            }
            else
            {
                data.Age = 50; // Unknown age — treat as very old (matches NugetMetadataService behaviour)
            }
        }
    }
}
