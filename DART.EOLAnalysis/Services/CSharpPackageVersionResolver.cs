using System.Text.RegularExpressions;
using System.Xml.Linq;
using DART.EOLAnalysis.Models;
using Microsoft.Extensions.Logging;

namespace DART.EOLAnalysis.Services
{
    public class CSharpPackageVersionResolver : ICSharpPackageVersionResolver
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyPackageVersions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex GetPathOfFileAboveRegex = new(
            @"^\$\(\[MSBuild\]::GetPathOfFileAbove\(\s*['""](?<file>[^'""]+)['""]\s*,\s*['""](?<start>[^'""]+)['""]\s*\)\s*\)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ILogger<CSharpPackageVersionResolver> _logger;

        public CSharpPackageVersionResolver(ILogger<CSharpPackageVersionResolver> logger)
        {
            _logger = logger;
        }

        public List<(string Id, string Version)> ResolvePackageVersions(ProjectInfo projectInfo)
        {
            ArgumentNullException.ThrowIfNull(projectInfo);

            if (string.IsNullOrWhiteSpace(projectInfo.Content))
            {
                _logger.LogWarning("Project content for {ProjectPath} is empty. No package versions resolved.", projectInfo.FilePath);
                return [];
            }

            XDocument projectDocument;
            try
            {
                projectDocument = XDocument.Parse(projectInfo.Content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse project file {ProjectPath}. No package versions resolved.", projectInfo.FilePath);
                return [];
            }

            var cpmFallbackEnabled = !IsCpmFallbackDisabled(projectDocument, projectInfo.FilePath);
            var cpmVersionMap = cpmFallbackEnabled
                ? BuildCpmPackageVersionMap(projectInfo)
                : EmptyPackageVersions;

            var resolvedPackages = new List<(string Id, string Version)>();

            foreach (var packageReference in GetDescendantsByLocalName(projectDocument.Root, "PackageReference"))
            {
                if (!ShouldProcessCondition(GetAttributeValue(packageReference, "Condition"), "PackageReference", projectInfo.FilePath))
                {
                    continue;
                }

                var packageId = GetFirstNonEmpty(
                    GetAttributeValue(packageReference, "Include"),
                    GetAttributeValue(packageReference, "Update"));

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    _logger.LogWarning("Skipping PackageReference in {ProjectPath} because Include/Update is missing.", projectInfo.FilePath);
                    continue;
                }

                var directVersion = GetFirstNonEmpty(
                    GetAttributeOrChildElementValue(packageReference, "VersionOverride"),
                    GetAttributeOrChildElementValue(packageReference, "Version"));

                if (!string.IsNullOrWhiteSpace(directVersion))
                {
                    resolvedPackages.Add((packageId, directVersion));
                    continue;
                }

                if (!cpmFallbackEnabled)
                {
                    _logger.LogWarning("Package {PackageId} in {ProjectPath} has no direct version and CPM fallback is disabled.",
                        packageId, projectInfo.FilePath);
                    continue;
                }

                if (cpmVersionMap.TryGetValue(packageId, out var cpmVersion))
                {
                    resolvedPackages.Add((packageId, cpmVersion));
                    continue;
                }

                _logger.LogWarning("Could not resolve version for package {PackageId} in {ProjectPath}. The package will be skipped.",
                    packageId, projectInfo.FilePath);
            }

            return resolvedPackages;
        }

        private bool IsCpmFallbackDisabled(XDocument projectDocument, string projectPath)
        {
            foreach (var manageCpmProperty in GetDescendantsByLocalName(projectDocument.Root, "ManagePackageVersionsCentrally"))
            {
                if (!ShouldProcessCondition(GetAttributeValue(manageCpmProperty, "Condition"), "ManagePackageVersionsCentrally", projectPath))
                {
                    continue;
                }

                if (manageCpmProperty.Parent is XElement propertyGroup
                    && string.Equals(propertyGroup.Name.LocalName, "PropertyGroup", StringComparison.OrdinalIgnoreCase)
                    && !ShouldProcessCondition(GetAttributeValue(propertyGroup, "Condition"), "PropertyGroup", projectPath))
                {
                    continue;
                }

                if (TryParseBooleanLiteral(manageCpmProperty.Value, out var enabled) && !enabled)
                {
                    _logger.LogInformation("CPM fallback disabled for project {ProjectPath} because ManagePackageVersionsCentrally is false.", projectPath);
                    return true;
                }
            }

            return false;
        }

        private IReadOnlyDictionary<string, string> BuildCpmPackageVersionMap(ProjectInfo projectInfo)
        {
            if (projectInfo.DirectoryPackagesPropsByPath == null || projectInfo.DirectoryPackagesPropsByPath.Count == 0)
            {
                return EmptyPackageVersions;
            }

            var normalizedPropsByPath = NormalizePropsContext(projectInfo.DirectoryPackagesPropsByPath);

            if (normalizedPropsByPath.Count == 0)
            {
                return EmptyPackageVersions;
            }

            var nearestPropsPath = FindNearestDirectoryPackagesPropsPath(projectInfo.FilePath, normalizedPropsByPath.Keys);

            if (nearestPropsPath == null)
            {
                _logger.LogInformation("No applicable Directory.Packages.props was found for project {ProjectPath}.", projectInfo.FilePath);
                return EmptyPackageVersions;
            }

            var cache = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return ResolvePropsFile(nearestPropsPath, normalizedPropsByPath, cache, visiting);
        }

        private IReadOnlyDictionary<string, string> ResolvePropsFile(
            string propsPath,
            IReadOnlyDictionary<string, string> propsByPath,
            Dictionary<string, IReadOnlyDictionary<string, string>> cache,
            HashSet<string> visiting)
        {
            if (cache.TryGetValue(propsPath, out var cached))
            {
                return cached;
            }

            if (!propsByPath.TryGetValue(propsPath, out var propsContent))
            {
                _logger.LogWarning("Directory.Packages.props {PropsPath} was requested but is missing from CPM context.", propsPath);
                return EmptyPackageVersions;
            }

            if (!visiting.Add(propsPath))
            {
                _logger.LogWarning("Import cycle detected while resolving Directory.Packages.props at {PropsPath}.", propsPath);
                return EmptyPackageVersions;
            }

            try
            {
                XDocument propsDocument;
                try
                {
                    propsDocument = XDocument.Parse(propsContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Directory.Packages.props file {PropsPath}.", propsPath);
                    cache[propsPath] = EmptyPackageVersions;
                    return EmptyPackageVersions;
                }

                var effectiveVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var importElement in GetDescendantsByLocalName(propsDocument.Root, "Import"))
                {
                    if (!ShouldProcessCondition(GetAttributeValue(importElement, "Condition"), "Import", propsPath))
                    {
                        continue;
                    }

                    var importProject = GetAttributeValue(importElement, "Project");

                    if (string.IsNullOrWhiteSpace(importProject))
                    {
                        _logger.LogWarning("Skipping Import in {PropsPath} because Project attribute is missing.", propsPath);
                        continue;
                    }

                    foreach (var resolvedImportPath in ResolveImportPaths(propsPath, importProject, propsByPath))
                    {
                        var parentVersions = ResolvePropsFile(resolvedImportPath, propsByPath, cache, visiting);

                        foreach (var parentVersion in parentVersions)
                        {
                            effectiveVersions[parentVersion.Key] = parentVersion.Value;
                        }
                    }
                }

                foreach (var packageVersionElement in GetDescendantsByLocalName(propsDocument.Root, "PackageVersion"))
                {
                    if (!ShouldProcessCondition(GetAttributeValue(packageVersionElement, "Condition"), "PackageVersion", propsPath))
                    {
                        continue;
                    }

                    var packageId = GetFirstNonEmpty(
                        GetAttributeValue(packageVersionElement, "Include"),
                        GetAttributeValue(packageVersionElement, "Update"));

                    if (string.IsNullOrWhiteSpace(packageId))
                    {
                        _logger.LogWarning("Skipping PackageVersion in {PropsPath} because Include/Update is missing.", propsPath);
                        continue;
                    }

                    var packageVersion = GetAttributeOrChildElementValue(packageVersionElement, "Version");

                    if (string.IsNullOrWhiteSpace(packageVersion))
                    {
                        _logger.LogWarning("Skipping PackageVersion for {PackageId} in {PropsPath} because Version is missing.",
                            packageId, propsPath);
                        continue;
                    }

                    if (ContainsUnsupportedMsBuildExpression(packageVersion))
                    {
                        _logger.LogWarning("Skipping PackageVersion for {PackageId} in {PropsPath} because Version uses unsupported MSBuild expression {Version}.",
                            packageId, propsPath, packageVersion);
                        continue;
                    }

                    effectiveVersions[packageId] = packageVersion;
                }

                IReadOnlyDictionary<string, string> resolvedVersions = effectiveVersions.Count == 0
                    ? EmptyPackageVersions
                    : new Dictionary<string, string>(effectiveVersions, StringComparer.OrdinalIgnoreCase);

                cache[propsPath] = resolvedVersions;
                return resolvedVersions;
            }
            finally
            {
                visiting.Remove(propsPath);
            }
        }

        private IEnumerable<string> ResolveImportPaths(
            string currentPropsPath,
            string importProjectExpression,
            IReadOnlyDictionary<string, string> propsByPath)
        {
            var currentDirectory = GetDirectoryPath(currentPropsPath);
            var trimmedImportExpression = importProjectExpression.Trim();

            if (TryResolveGetPathOfFileAboveImport(trimmedImportExpression, currentDirectory, propsByPath, out var resolvedAbovePath))
            {
                if (!string.IsNullOrWhiteSpace(resolvedAbovePath))
                {
                    yield return resolvedAbovePath;
                }

                yield break;
            }

            var expandedImportPath = ExpandMsBuildThisFileDirectory(trimmedImportExpression, currentDirectory);

            if (ContainsUnsupportedMsBuildExpression(expandedImportPath))
            {
                _logger.LogWarning("Skipping Import path expression {ImportProjectExpression} in {PropsPath} because it uses unsupported MSBuild expressions.",
                    importProjectExpression, currentPropsPath);
                yield break;
            }

            var resolvedPath = NormalizeRepoPath(CombineRepoPath(currentDirectory, expandedImportPath));

            if (!propsByPath.ContainsKey(resolvedPath))
            {
                _logger.LogWarning("Import path {ResolvedPath} from {PropsPath} does not exist in CPM context.",
                    resolvedPath, currentPropsPath);
                yield break;
            }

            yield return resolvedPath;
        }

        private bool TryResolveGetPathOfFileAboveImport(
            string importExpression,
            string currentDirectory,
            IReadOnlyDictionary<string, string> propsByPath,
            out string? resolvedPath)
        {
            resolvedPath = null;

            var match = GetPathOfFileAboveRegex.Match(importExpression);
            if (!match.Success)
            {
                return false;
            }

            var fileToFind = match.Groups["file"].Value.Trim();
            var startPathExpression = match.Groups["start"].Value.Trim();

            if (string.IsNullOrWhiteSpace(fileToFind) || string.IsNullOrWhiteSpace(startPathExpression))
            {
                _logger.LogWarning("GetPathOfFileAbove import expression {ImportExpression} in {CurrentDirectory} is invalid.",
                    importExpression, currentDirectory);
                return true;
            }

            var expandedStartPath = ExpandMsBuildThisFileDirectory(startPathExpression, currentDirectory);

            if (ContainsUnsupportedMsBuildExpression(expandedStartPath))
            {
                _logger.LogWarning("GetPathOfFileAbove start path {StartPathExpression} in {CurrentDirectory} uses unsupported MSBuild expressions.",
                    startPathExpression, currentDirectory);
                return true;
            }

            var searchStartDirectory = NormalizeDirectoryPath(CombineRepoPath(currentDirectory, expandedStartPath));
            resolvedPath = FindPathOfFileAbove(fileToFind, searchStartDirectory, propsByPath.Keys);

            if (resolvedPath == null)
            {
                _logger.LogWarning("GetPathOfFileAbove could not resolve file {FileToFind} starting from {SearchStartDirectory}.",
                    fileToFind, searchStartDirectory);
            }

            return true;
        }

        private static IReadOnlyDictionary<string, string> NormalizePropsContext(IReadOnlyDictionary<string, string> propsByPath)
        {
            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in propsByPath)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                var normalizedPath = NormalizeRepoPath(entry.Key);

                if (!IsDirectoryPackagesPropsFile(normalizedPath))
                {
                    continue;
                }

                normalized[normalizedPath] = entry.Value;
            }

            return normalized;
        }

        private string? FindNearestDirectoryPackagesPropsPath(string projectFilePath, IEnumerable<string> propsPaths)
        {
            var normalizedProjectPath = NormalizeRepoPath(projectFilePath);
            var projectDirectory = GetDirectoryPath(normalizedProjectPath);
            string? nearestPath = null;
            var nearestDepth = -1;

            foreach (var propsPath in propsPaths)
            {
                var normalizedPropsPath = NormalizeRepoPath(propsPath);
                var propsDirectory = GetDirectoryPath(normalizedPropsPath);

                if (!IsAncestorOrSamePath(propsDirectory, projectDirectory))
                {
                    continue;
                }

                var depth = GetPathDepth(propsDirectory);
                if (depth > nearestDepth)
                {
                    nearestPath = normalizedPropsPath;
                    nearestDepth = depth;
                    continue;
                }

                if (depth == nearestDepth
                    && nearestPath != null
                    && string.Compare(normalizedPropsPath, nearestPath, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    nearestPath = normalizedPropsPath;
                }
            }

            return nearestPath;
        }

        private bool ShouldProcessCondition(string? condition, string elementName, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            if (TryParseBooleanLiteral(condition, out var value) && value)
            {
                return true;
            }

            _logger.LogWarning("Skipping {ElementName} in {SourcePath} because Condition {Condition} is not supported.",
                elementName, sourcePath, condition);
            return false;
        }

        private static bool TryParseBooleanLiteral(string? input, out bool value)
        {
            value = false;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var normalized = input.Trim();

            while (normalized.Length >= 2
                && normalized.StartsWith("(", StringComparison.Ordinal)
                && normalized.EndsWith(")", StringComparison.Ordinal))
            {
                normalized = normalized[1..^1].Trim();
            }

            if (normalized.Length >= 2
                && ((normalized.StartsWith("\"", StringComparison.Ordinal) && normalized.EndsWith("\"", StringComparison.Ordinal))
                    || (normalized.StartsWith("'", StringComparison.Ordinal) && normalized.EndsWith("'", StringComparison.Ordinal))))
            {
                normalized = normalized[1..^1].Trim();
            }

            return bool.TryParse(normalized, out value);
        }

        private static IEnumerable<XElement> GetDescendantsByLocalName(XElement? root, string localName)
        {
            if (root == null)
            {
                return Enumerable.Empty<XElement>();
            }

            return root.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetAttributeValue(XElement element, string attributeName)
        {
            var attribute = element.Attributes()
                .FirstOrDefault(a => string.Equals(a.Name.LocalName, attributeName, StringComparison.OrdinalIgnoreCase));

            return NormalizeOptionalString(attribute?.Value);
        }

        private static string? GetAttributeOrChildElementValue(XElement element, string name)
        {
            var attributeValue = GetAttributeValue(element, name);
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                return attributeValue;
            }

            var childElement = element.Elements()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));

            return NormalizeOptionalString(childElement?.Value);
        }

        private static string? GetFirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static bool ContainsUnsupportedMsBuildExpression(string value)
        {
            return value.Contains("$(", StringComparison.Ordinal)
                || value.Contains("$[", StringComparison.Ordinal);
        }

        private static string ExpandMsBuildThisFileDirectory(string value, string currentDirectory)
        {
            var normalizedCurrentDirectory = NormalizeDirectoryPath(currentDirectory);
            if (!normalizedCurrentDirectory.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedCurrentDirectory += "/";
            }

            return value.Replace("$(MSBuildThisFileDirectory)", normalizedCurrentDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string? FindPathOfFileAbove(string fileName, string startDirectory, IEnumerable<string> candidatePaths)
        {
            var lookup = new HashSet<string>(candidatePaths.Select(NormalizeRepoPath), StringComparer.OrdinalIgnoreCase);
            var currentDirectory = NormalizeDirectoryPath(startDirectory);

            while (true)
            {
                var candidate = NormalizeRepoPath(CombineRepoPath(currentDirectory, fileName));
                if (lookup.Contains(candidate))
                {
                    return candidate;
                }

                if (string.Equals(currentDirectory, "/", StringComparison.Ordinal))
                {
                    return null;
                }

                currentDirectory = GetDirectoryPath(currentDirectory);
            }
        }

        private static bool IsDirectoryPackagesPropsFile(string path)
        {
            var fileName = GetFileName(path);
            return string.Equals(fileName, "Directory.Packages.props", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAncestorOrSamePath(string ancestorPath, string candidatePath)
        {
            var normalizedAncestorPath = NormalizeRepoPath(ancestorPath);
            var normalizedCandidatePath = NormalizeRepoPath(candidatePath);

            if (string.Equals(normalizedAncestorPath, "/", StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(normalizedAncestorPath, normalizedCandidatePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedCandidatePath.StartsWith(normalizedAncestorPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetPathDepth(string path)
        {
            var normalized = NormalizeRepoPath(path);
            return normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string GetFileName(string path)
        {
            var normalized = NormalizeRepoPath(path);
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length == 0 ? string.Empty : segments[^1];
        }

        private static string GetDirectoryPath(string path)
        {
            var normalized = NormalizeRepoPath(path);
            var lastSlashIndex = normalized.LastIndexOf('/');

            if (lastSlashIndex <= 0)
            {
                return "/";
            }

            return normalized[..lastSlashIndex];
        }

        private static string NormalizeDirectoryPath(string path)
        {
            var normalized = NormalizeRepoPath(path);

            if (path.EndsWith("/", StringComparison.Ordinal) || path.EndsWith("\\", StringComparison.Ordinal))
            {
                return normalized;
            }

            var fileName = GetFileName(normalized);

            if (fileName.Contains('.', StringComparison.Ordinal))
            {
                return GetDirectoryPath(normalized);
            }

            return normalized;
        }

        private static string CombineRepoPath(string baseDirectory, string relativeOrAbsolutePath)
        {
            var normalizedCandidate = (relativeOrAbsolutePath ?? string.Empty).Replace('\\', '/');

            if (normalizedCandidate.StartsWith("/", StringComparison.Ordinal))
            {
                return normalizedCandidate;
            }

            var normalizedBaseDirectory = NormalizeRepoPath(baseDirectory);

            if (string.Equals(normalizedBaseDirectory, "/", StringComparison.Ordinal))
            {
                return "/" + normalizedCandidate;
            }

            return normalizedBaseDirectory + "/" + normalizedCandidate;
        }

        private static string NormalizeRepoPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            var normalizedSeparators = path.Replace('\\', '/');
            if (!normalizedSeparators.StartsWith("/", StringComparison.Ordinal))
            {
                normalizedSeparators = "/" + normalizedSeparators;
            }

            var segments = normalizedSeparators.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var normalizedSegments = new List<string>(segments.Length);

            foreach (var segment in segments)
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (normalizedSegments.Count > 0)
                    {
                        normalizedSegments.RemoveAt(normalizedSegments.Count - 1);
                    }
                    continue;
                }

                normalizedSegments.Add(segment);
            }

            return normalizedSegments.Count == 0
                ? "/"
                : "/" + string.Join("/", normalizedSegments);
        }

        private static string? NormalizeOptionalString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}
