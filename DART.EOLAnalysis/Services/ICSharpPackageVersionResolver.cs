namespace DART.EOLAnalysis
{
    /// <summary>
    /// Resolves C# package versions using direct project versions first, then CPM fallback when available.
    /// </summary>
    public interface ICSharpPackageVersionResolver
    {
        /// <summary>
        /// Resolves package IDs and versions for a C# project file.
        /// </summary>
        /// <param name="projectInfo">Project file content and optional Directory.Packages.props context.</param>
        /// <returns>Resolved package ID and version pairs.</returns>
        List<(string Id, string Version)> ResolvePackageVersions(ProjectInfo projectInfo);
    }
}
