using DART.BlackduckAnalysis;
using DART.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DART.Tests.DART.BlackduckAnalysis.Services;

public sealed class BlackduckAnalyzerAdapterTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldCollectFindings_FromDownloadedCsv()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dart-blackduck-analyzer-tests", Guid.NewGuid().ToString("N"));
        var downloadsDir = Path.Combine(tempDir, BlackduckConfiguration.DownloadsFolderName);
        Directory.CreateDirectory(downloadsDir);
        var csvPath = Path.Combine(downloadsDir, "report.csv");
        await File.WriteAllTextAsync(
            csvPath,
            """
            Project id,Project name,Component origin id,Security Risk,Vulnerability ID,Match type,Version
            proj-1,APP01,Newtonsoft.Json,HIGH,CVE-2024-10001,Direct Dependency,13.0.3
            """);

        var blackduckConfiguration = new BlackduckConfiguration
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
        };

        var reportConfiguration = new ReportConfiguration
        {
            OutputFilePath = tempDir,
            ProductName = "Product",
            ProductVersion = "1.0",
            ProductIteration = "Sprint1"
        };

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
            Options.Create(blackduckConfiguration),
            Options.Create(reportConfiguration),
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
}
