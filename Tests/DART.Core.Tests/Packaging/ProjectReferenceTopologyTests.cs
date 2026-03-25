using System.Xml.Linq;

namespace DART.Tests.Packaging;

public class ProjectReferenceTopologyTests
{
    [Fact]
    public void ConsoleProject_ShouldReferenceCoreAndReportGenerator_AndNotReferenceAnalyzerProjectsDirectly()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "Src", "DART.Console", "DART.Console.csproj"));

        Assert.Contains("..\\DART.Core\\DART.Core.csproj", projectReferences);
        Assert.Contains("..\\DART.ReportGenerator\\DART.ReportGenerator.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Fact]
    public void CoreProject_ShouldReferenceAnalyzerProjects()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "Src", "DART.Core", "DART.Core.csproj"));

        Assert.Contains("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.Contains("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Fact]
    public void ReportGeneratorProject_ShouldNotReferenceAnalyzerProjects()
    {
        var projectReferences = GetProjectReferences(Path.Combine(RepoRoot(), "Src", "DART.ReportGenerator", "DART.ReportGenerator.csproj"));

        Assert.DoesNotContain("..\\DART.BlackduckAnalysis\\DART.BlackduckAnalysis.csproj", projectReferences);
        Assert.DoesNotContain("..\\DART.EOLAnalysis\\DART.EOLAnalysis.csproj", projectReferences);
    }

    [Fact]
    public void CoreProject_ShouldNotContainBlackduckCollectorSources()
    {
        var collectorDirectory = Path.Combine(RepoRoot(), "Src", "DART.Core", "Blackduck");
        var containsSourceFiles = Directory.Exists(collectorDirectory)
            && Directory.GetFiles(collectorDirectory, "*.cs", SearchOption.AllDirectories).Length > 0;

        Assert.False(containsSourceFiles);
    }

    [Fact]
    public void BlackduckAnalysisProject_ShouldContainBlackduckCollectorSources()
    {
        var blackduckDirectory = Path.Combine(RepoRoot(), "Src", "DART.BlackduckAnalysis", "Collectors");
        var hasCollector = File.Exists(Path.Combine(blackduckDirectory, "BlackduckFindingCollector.cs"));
        var hasCollectorContract = File.Exists(Path.Combine(blackduckDirectory, "IBlackduckFindingCollector.cs"));

        Assert.True(hasCollector && hasCollectorContract);
    }

    [Theory]
    [InlineData("Src/DART.Core/DART.Core.csproj", "DART.Core")]
    [InlineData("Src/DART.BlackduckAnalysis/DART.BlackduckAnalysis.csproj", "DART.BlackduckAnalysis")]
    [InlineData("Src/DART.EOLAnalysis/DART.EOLAnalysis.csproj", "DART.EOLAnalysis")]
    [InlineData("Src/DART.ReportGenerator/DART.ReportGenerator.csproj", "DART.ReportGenerator")]
    public void LibraryProjects_ShouldDeclareExpectedPackageId(string relativeProjectPath, string expectedPackageId)
    {
        var packageId = GetSingleProperty(Path.Combine(RepoRoot(), relativeProjectPath), "PackageId");

        Assert.Equal(expectedPackageId, packageId);
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Src"))
                && Directory.Exists(Path.Combine(current.FullName, "Tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Src and Tests folders.");
    }

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
