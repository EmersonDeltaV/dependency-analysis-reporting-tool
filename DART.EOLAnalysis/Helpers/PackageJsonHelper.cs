using System.Text.Json;

namespace DART.EOLAnalysis.Helpers
{
    /// <summary>
    /// Parses npm package dependencies from package.json file content.
    /// </summary>
    public static class PackageJsonHelper
    {
        /// <summary>
        /// Strips semver range prefixes (^, ~, >=, >, <=, &lt;, =) and surrounding whitespace
        /// from a version string so that it can be used to look up an exact version.
        /// Examples: "^1.2.3" → "1.2.3", "~2.0.0" → "2.0.0", ">=3.1.0" → "3.1.0"
        /// </summary>
        public static string StripSemverPrefix(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return version;

            return version.TrimStart('^', '~', '>', '<', '=', ' ').Trim();
        }

        /// <summary>
        /// Extracts all package name/version pairs from the "dependencies" and
        /// "devDependencies" sections of a package.json file.
        /// Semver range prefixes (^, ~, >=, etc.) are preserved in the Version field
        /// and stripped later by the metadata service when querying the registry.
        /// </summary>
        /// <param name="content">The raw text content of a package.json file.</param>
        /// <param name="includeDevDependencies">When <c>true</c>, entries from "devDependencies" are included. Defaults to <c>true</c>.</param>
        /// <returns>An enumerable of (Name, Version) tuples.</returns>
        /// <exception cref="ArgumentException">Thrown when content is null or empty.</exception>
        /// <exception cref="JsonException">Thrown when the content is not valid JSON.</exception>
        public static IEnumerable<(string Name, string Version)> GetPackagesFromContent(string content, bool includeDevDependencies = false)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be null or empty.", nameof(content));
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var packages = new List<(string Name, string Version)>();

            ExtractDependencySection(root, "dependencies", packages);

            if (includeDevDependencies)
                ExtractDependencySection(root, "devDependencies", packages);

            return packages;
        }

        private static void ExtractDependencySection(
            JsonElement root,
            string sectionName,
            List<(string Name, string Version)> packages)
        {
            if (!root.TryGetProperty(sectionName, out var section))
                return;

            foreach (var property in section.EnumerateObject())
            {
                var name = property.Name;
                var version = property.Value.GetString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                {
                    packages.Add((name, version));
                }
            }
        }
    }
}
