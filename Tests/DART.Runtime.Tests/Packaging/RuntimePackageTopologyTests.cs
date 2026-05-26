using System.Xml.Linq;
using DART.Runtime;

namespace DART.Runtime.Tests.Packaging;

[Trait("Category", "Unit")]
public sealed class RuntimePackageTopologyTests
{
    [Fact]
    public void RuntimePackage_ShouldExposePublicExecutionContracts()
    {
        Assert.Equal("DART.Runtime", typeof(DartExecutionRequest).Namespace);
        Assert.Equal("DART.Runtime", typeof(IDartExecutionRunner).Namespace);
    }

    [Fact]
    public void RuntimeProject_ShouldDeclareExpectedPackageMetadata()
    {
        var projectPath = Path.Combine(GetRepositoryRoot(), "Src", "DART.Runtime", "DART.Runtime.csproj");
        var document = XDocument.Load(projectPath);
        var propertyGroup = document.Root?.Elements("PropertyGroup").FirstOrDefault();

        Assert.NotNull(propertyGroup);
        Assert.Equal("DART.Runtime", propertyGroup!.Element("PackageId")?.Value);
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}