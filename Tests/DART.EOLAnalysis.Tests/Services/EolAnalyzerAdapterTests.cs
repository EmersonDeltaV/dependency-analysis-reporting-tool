using DART.Core;
using DART.EOLAnalysis;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DART.Tests.DART.EOLAnalysis.Services;

public sealed class EolAnalyzerAdapterTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldMapPackageData_ToEolFindings()
    {
        var eolAnalysisService = Substitute.For<IEOLAnalysisService>();

        eolAnalysisService
            .AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<EolFeatureToggles>(), Arg.Any<CancellationToken>())
            .Returns(new List<PackageData>
            {
                new()
                {
                    Id = "Newtonsoft.Json",
                    Repository = "Repo-1",
                    Project = "Project-1",
                    Version = "12.0.3",
                    VersionDate = "2021-01-01",
                    Age = 365,
                    LatestVersion = "13.0.3",
                    LatestVersionDate = "2026-01-01",
                    License = "MIT",
                    LicenseUrl = "https://example.com/license",
                    Action = "Update to newer version"
                }
            });

        var sut = new EolAnalyzerAdapter(
            eolAnalysisService,
            Options.Create(new EOLAnalysisConfig
            {
                Repositories =
                [
                    new Repository
                    {
                        Name = "Repo-1",
                        Url = "https://dev.azure.com/Org/Proj/_git/Repo-1",
                        Branch = "main"
                    }
                ]
            }),
            Options.Create(new FeatureToggles
            {
                EnableBlackduckAnalysis = true,
                EnableCSharpAnalysis = true,
                EnableNpmAnalysis = false
            }));

        var result = await sut.AnalyzeAsync(
            new AnalysisRequest { EnableBlackduckAnalysis = false, EnableEolAnalysis = true },
            CancellationToken.None);

        Assert.Single(result);
        var finding = result.First();
        Assert.Equal("Newtonsoft.Json", finding.PackageId);
        Assert.Equal("Repo-1", finding.Repository);
        Assert.Equal("2021-01-01", finding.VersionDate);
        Assert.Equal(365, finding.AgeDays);
        Assert.Equal("Update to newer version", finding.RecommendedAction);
    }
}
