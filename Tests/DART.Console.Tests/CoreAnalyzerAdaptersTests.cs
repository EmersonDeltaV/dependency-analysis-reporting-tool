using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;
using DART.Console.Services.Implementation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DART.Tests.DART.Console;

public sealed class CoreAnalyzerAdaptersTests
{
    [Fact]
    public async Task BlackduckAnalyzerAdapter_ShouldCollectFindings_FromDownloadedCsv()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dart-core-analyzer-tests", Guid.NewGuid().ToString("N"));
        var downloadsDir = Path.Combine(tempDir, BlackduckConfiguration.DownloadsFolderName);
        Directory.CreateDirectory(downloadsDir);
        var csvPath = Path.Combine(downloadsDir, "report.csv");
        await File.WriteAllTextAsync(csvPath,
            """
            Project id,Project name,Component origin id,Security Risk,Vulnerability ID,Match type,Version
            proj-1,APP01,Newtonsoft.Json,HIGH,CVE-2024-10001,Direct Dependency,13.0.3
            """);

        var config = CreateConfig(tempDir);
        var options = Options.Create(config);

        var blackduckReportGenerator = Substitute.For<IBlackduckReportGenerator>();
        var blackduckApiService = Substitute.For<IBlackduckApiService>();
        var logger = Substitute.For<ILogger<BlackduckAnalyzerAdapter>>();

        blackduckApiService
            .GetLatestProjectVersion(Arg.Any<BlackduckConfiguration>())
            .Returns(new Dictionary<string, string> { ["proj-1"] = "13.0.3" });

        blackduckApiService
            .GetRecommendedFix(Arg.Any<BlackduckConfiguration>(), "CVE-2024-10001")
            .Returns("13.0.4");

        var sut = new BlackduckAnalyzerAdapter(
            blackduckReportGenerator,
            blackduckApiService,
            options,
            new BlackduckFindingCollector(),
            logger);

        try
        {
            var result = await sut.AnalyzeAsync(
                new AnalysisRequest { EnableBlackduckAnalysis = true, EnableEolAnalysis = false },
                CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("APP01", result.First().ApplicationName);
            Assert.Equal("CVE-2024-10001", result.First().VulnerabilityId);
            Assert.Equal("13.0.4", result.First().RecommendedFix);

            await blackduckReportGenerator.Received(1).GenerateReport();
            await blackduckReportGenerator.Received(1).Cleanup();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task EolAnalyzerAdapter_ShouldMapPackageData_ToEolFindings()
    {
        var config = CreateConfig(@"C:\Output");
        var options = Options.Create(config);
        var eolService = Substitute.For<IEOLAnalysisService>();

        eolService.AnalyzeRepositoriesAsync(Arg.Any<EOLAnalysisConfig>(), Arg.Any<FeatureToggles>(), Arg.Any<CancellationToken>())
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

        var sut = new EolAnalyzerAdapter(eolService, options);

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

    private static Config CreateConfig(string outputPath)
        => new()
        {
            ReportConfiguration = new ReportConfiguration
            {
                OutputFilePath = outputPath,
                ProductName = "Product",
                ProductVersion = "1.0",
                ProductIteration = "Sprint1"
            },
            BlackduckConfiguration = new BlackduckConfiguration
            {
                BaseUrl = "https://example.blackduck.com",
                Token = "token",
                IncludeRecommendedFix = true,
                IncludeTransitiveDependency = true,
                BlackduckRepositories =
                [
                    new BlackduckRepository
                    {
                        Name = "APP01",
                        Id = "proj-1",
                        Versions = "13.0.3"
                    }
                ]
            },
            FeatureToggles = new FeatureToggles
            {
                EnableBlackduckAnalysis = true,
                EnableCSharpAnalysis = true,
                EnableNpmAnalysis = false
            },
            EOLAnalysis = new EOLAnalysisConfig
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
            }
        };
}

