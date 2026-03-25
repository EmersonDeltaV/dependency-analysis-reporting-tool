using System.Xml.Linq;

namespace DART.Tests.Packaging;

public class ProjectReferenceTopologyTests
{
    [Fact]
    public void ConsoleProject_ShouldReferenceCoreAndReportGenerator_AndNotReferenceAnalyzerProjectsDirectly()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "DART", "DART.csproj"));

        Assert.Contains("..\\DART.Core\\DART.Core.csproj", projectReferences);
        Assert.Contains("..\\DART.ReportGenerator\\DART.ReportGenerator.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Fact]
    public void CoreProject_ShouldReferenceAnalyzerProjects()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "DART.Core", "DART.Core.csproj"));

        Assert.Contains("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.Contains("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Fact]
    public void ReportGeneratorProject_ShouldNotReferenceAnalyzerProjects()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "DART.ReportGenerator", "DART.ReportGenerator.csproj"));

        Assert.DoesNotContain("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Theory]
    [InlineData("DART.Core/DART.Core.csproj", "DART.Core")]
    [InlineData("DART.BlackduckAnalysis/DART.BlackduckAnalysis.csproj", "DART.BlackduckAnalysis")]
    [InlineData("DART.EOLAnalysis/DART.EOLAnalysis.csproj", "DART.EOLAnalysis")]
    [InlineData("DART.ReportGenerator/DART.ReportGenerator.csproj", "DART.ReportGenerator")]
    public void LibraryProjects_ShouldDeclareExpectedPackageId(string relativeProjectPath, string expectedPackageId)
    {
        var packageId = GetSingleProperty(Path.Combine(RepoRoot(), relativeProjectPath), "PackageId");

        Assert.Equal(expectedPackageId, packageId);
    }

    private static string RepoRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static IReadOnlyList<string> GetProjectReferences(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants("ProjectReference")
            .Select(node => (string?)node.Attribute("Include"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private static string? GetSingleProperty(string projectPath, string propertyName)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants(propertyName).Select(x => x.Value).FirstOrDefault();
    }
}
