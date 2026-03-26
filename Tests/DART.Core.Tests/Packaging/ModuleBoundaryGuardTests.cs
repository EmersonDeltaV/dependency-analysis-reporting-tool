using System.Xml.Linq;

namespace DART.Core.Tests.Packaging;

public sealed class ModuleBoundaryGuardTests
{
    [Theory]
    [MemberData(nameof(ForbiddenReferences))]
    public void Project_ShouldNotReferenceForbiddenProjects(string projectRelativePath, string boundaryDescription, string[] forbiddenProjectReferences)
    {
        var projectPath = Path.Combine(RepoRoot(), projectRelativePath);
        var projectReferences = GetProjectReferences(projectPath);

        var violatingReferences = forbiddenProjectReferences
            .Where(projectReferences.Contains)
            .ToArray();

        Assert.True(
            violatingReferences.Length == 0,
            $"{boundaryDescription} violation in '{projectRelativePath}'. Forbidden references found: {string.Join(", ", violatingReferences)}. Actual references: {string.Join(", ", projectReferences)}");
    }

    public static IEnumerable<object[]> ForbiddenReferences()
    {
        yield return new object[]
        {
            Path.Combine("Src", "DART.BlackduckAnalysis", "DART.BlackduckAnalysis.csproj"),
            "DART.BlackduckAnalysis must stay independent from EOLAnalysis",
            new[] { @"..\DART.EOLAnalysis\DART.EOLAnalysis.csproj" }
        };

        yield return new object[]
        {
            Path.Combine("Src", "DART.BlackduckAnalysis", "DART.BlackduckAnalysis.csproj"),
            "DART.BlackduckAnalysis must stay independent from the report generator",
            new[] { @"..\DART.ReportGenerator\DART.ReportGenerator.csproj" }
        };

        yield return new object[]
        {
            Path.Combine("Src", "DART.EOLAnalysis", "DART.EOLAnalysis.csproj"),
            "DART.EOLAnalysis must stay independent from BlackduckAnalysis and the report generator",
            new[] { @"..\DART.BlackduckAnalysis\DART.BlackduckAnalysis.csproj", @"..\DART.ReportGenerator\DART.ReportGenerator.csproj" }
        };

        yield return new object[]
        {
            Path.Combine("Src", "DART.ReportGenerator", "DART.ReportGenerator.csproj"),
            "DART.ReportGenerator must remain output-only and cannot depend on analyzer modules",
            new[] { @"..\DART.BlackduckAnalysis\DART.BlackduckAnalysis.csproj", @"..\DART.EOLAnalysis\DART.EOLAnalysis.csproj" }
        };

        yield return new object[]
        {
            Path.Combine("Src", "DART.Core", "DART.Core.csproj"),
            "DART.Core must have no DART project dependencies",
            new[]
            {
                @"..\DART.Console\DART.Console.csproj",
                @"..\DART.ReportGenerator\DART.ReportGenerator.csproj",
                @"..\DART.BlackduckAnalysis\DART.BlackduckAnalysis.csproj",
                @"..\DART.EOLAnalysis\DART.EOLAnalysis.csproj"
            }
        };
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
}
