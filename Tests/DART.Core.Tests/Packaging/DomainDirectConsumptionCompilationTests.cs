using System.Xml.Linq;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;

namespace DART.Core.Tests.Packaging;

public sealed class DomainDirectConsumptionCompilationTests
{
    [Fact]
    public void DomainPackages_ShouldExposePublicTypesForDirectConsumption()
    {
        var blackduckConfiguration = new BlackduckConfiguration();
        var eolAnalysisConfig = new EOLAnalysisConfig();

        Assert.NotNull(blackduckConfiguration);
        Assert.NotNull(eolAnalysisConfig);
        Assert.Equal(typeof(IBlackduckReportService).Namespace, typeof(BlackduckConfiguration).Namespace);
        Assert.Equal("DART.EOLAnalysis", typeof(IEOLAnalysisService).Namespace);
        Assert.Equal("DART.EOLAnalysis", typeof(EOLAnalysisConfig).Namespace);
    }

    [Theory]
    [InlineData("Src/DART.BlackduckAnalysis/DART.BlackduckAnalysis.csproj", "DART.BlackduckAnalysis")]
    [InlineData("Src/DART.EOLAnalysis/DART.EOLAnalysis.csproj", "DART.EOLAnalysis")]
    [InlineData("Src/DART.Core/DART.Core.csproj", "DART.Core")]
    [InlineData("Src/DART.ReportGenerator/DART.ReportGenerator.csproj", "DART.ReportGenerator")]
    public void LibraryProjects_ShouldDeclarePackMetadata(string relativeProjectPath, string expectedPackageId)
    {
        var projectPath = Path.Combine(GetRepositoryRoot(), relativeProjectPath);
        var document = XDocument.Load(projectPath);
        var propertyGroup = document.Root?.Elements("PropertyGroup").FirstOrDefault();

        Assert.NotNull(propertyGroup);
        Assert.Equal(expectedPackageId, propertyGroup!.Element("PackageId")?.Value);
        Assert.Equal("true", propertyGroup.Element("IsPackable")?.Value);
        Assert.False(string.IsNullOrWhiteSpace(propertyGroup.Element("Description")?.Value));
    }

    private static string GetRepositoryRoot()
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
}
