using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DART.Tests.DART.EOLAnalysis.Services
{
    public class ProjectAnalysisServiceTests
    {
        private static ProjectInfo CreateProjectWithPackages(params (string Id, string Version)[] packages)
        {
            var itemGroup = string.Empty;
            foreach (var p in packages)
            {
                itemGroup += $"<PackageReference Include=\"{p.Id}\" Version=\"{p.Version}\" />";
            }

            var content = $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{itemGroup}</ItemGroup></Project>";

            return new ProjectInfo
            {
                Name = "TestProject",
                FilePath = "TestProject.csproj",
                Content = content,
                RepositoryName = "Repo"
            };
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldMatchCaseInsensitiveAndQuestionMarkWildcard_AndIgnoreBlankPatterns()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var config = new PackageRecommendationConfig
            {
                // Lowercase pattern to validate case-insensitive matching, and '*' wildcard
                SkipInternalPackagesFilter = new List<string> { "  ", "emerson.uti*  " },
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Util", "1.2.3"), ("Emerson.Utility", "2.0.0"), ("Other", "1.0.0"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, CancellationToken.None);

            // Assert
            // emerson.uti? should match both Util and Utility (case-insensitive, '?' wildcard)
            var skippedIds = new HashSet<string>(new[] { "Emerson.Util", "Emerson.Utility" });
            Assert.Equal(3, result.Count);
            Assert.All(result.Where(r => skippedIds.Contains(r.Id)), r => Assert.Equal("INTERNAL", r.Action));
            Assert.Contains(result, r => r.Id == "Other" && r.Action == "ACTION");

            // Metadata should not be fetched for skipped ones
            await nuget.DidNotReceive().GetDataAsync(Arg.Is<PackageData>(p => skippedIds.Contains(p.Id)), Arg.Any<CancellationToken>());
            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Other"), Arg.Any<CancellationToken>());

            // DetermineAction should be called only for the non-skipped package
            rec.Received(1).DetermineAction(Arg.Is<PackageData>(p => p.Id == "Other"));
            rec.DidNotReceive().DetermineAction(Arg.Is<PackageData>(p => skippedIds.Contains(p.Id)));
        }
        private static (ProjectAnalysisService svc, INugetMetadataService nuget, IPackageRecommendationService rec, ILogger<ProjectAnalysisService> logger)
            CreateService()
        {
            var logger = Substitute.For<ILogger<ProjectAnalysisService>>();
            var nuget = Substitute.For<INugetMetadataService>();
            var rec = Substitute.For<IPackageRecommendationService>();
            var svc = new ProjectAnalysisService(logger, nuget, rec);
            return (svc, nuget, rec, logger);
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldSkipInternalPackagesUsingPatterns_AndSetSkipMessage()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var config = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = new List<string> { "Emerson.*" },
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            // For non-internal packages, DetermineAction returns a value
            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"), ("Newtonsoft.Json", "13.0.1"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            var internalPkg = result.Find(p => p.Id == "Emerson.Core");
            Assert.NotNull(internalPkg);
            Assert.Equal("INTERNAL", internalPkg!.Action);

            await nuget.DidNotReceive().GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"), Arg.Any<CancellationToken>());
            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"), Arg.Any<CancellationToken>());

            rec.Received(1).Initialize(config);
            rec.Received(1).DetermineAction(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"));
            rec.DidNotReceive().DetermineAction(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"));
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldNotSkip_WhenNoPatternsProvided()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var config = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = new List<string>(),
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"), ("Newtonsoft.Json", "13.0.1"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"), Arg.Any<CancellationToken>());
            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"), Arg.Any<CancellationToken>());

            rec.Received(1).Initialize(config);
            rec.Received(2).DetermineAction(Arg.Any<PackageData>());

            // None should be marked with INTERNAL skip message
            Assert.DoesNotContain(result, p => p.Action == "INTERNAL");
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldNotSkip_WhenPatternsAreNull()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var config = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = null!,
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("Emerson.Core", result[0].Id);
            Assert.Equal("ACTION", result[0].Action);
            await nuget.Received(1).GetDataAsync(Arg.Any<PackageData>(), Arg.Any<CancellationToken>());
            rec.Received(1).DetermineAction(Arg.Any<PackageData>());
        }
    }
}
