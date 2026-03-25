using System.Xml.Linq;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using DART.EOLAnalysis.Models;

namespace DART.Tests.Packaging;

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
        Assert.Equal("DART.EOLAnalysis.Models", typeof(EOLAnalysisConfig).Namespace);
    }

    [Theory]
    [InlineData("DART.BlackduckAnalysis/DART.BlackduckAnalysis.csproj", "DART.BlackduckAnalysis")]
    [InlineData("DART.EOLAnalysis/DART.EOLAnalysis.csproj", "DART.EOLAnalysis")]
    [InlineData("DART.Core/DART.Core.csproj", "DART.Core")]
    [InlineData("DART.ReportGenerator/DART.ReportGenerator.csproj", "DART.ReportGenerator")]
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
        var currentDirectory = Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(currentDirectory, "..", "..", "..", ".."));
    }
}
