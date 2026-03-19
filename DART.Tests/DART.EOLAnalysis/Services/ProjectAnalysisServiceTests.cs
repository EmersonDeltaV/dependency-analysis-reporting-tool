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

        private static ProjectInfo CreateProjectWithContent(string filePath, string content, IReadOnlyDictionary<string, string>? propsByPath = null)
        {
            return new ProjectInfo
            {
                Name = "TestProject",
                FilePath = filePath,
                Content = content,
                RepositoryName = "Repo",
                DirectoryPackagesPropsByPath = propsByPath ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldResolveCpmVersionAndAnalyzePackage_WhenInlineVersionMissing()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Serilog" />
              </ItemGroup>
            </Project>
            """;

            var propsContent = """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Serilog" Version="3.0.0" />
              </ItemGroup>
            </Project>
            """;

            var project = CreateProjectWithContent(
                "/src/App/App.csproj",
                projectContent,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/src/Directory.Packages.props"] = propsContent
                });

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("Serilog", result[0].Id);
            Assert.Equal("3.0.0", result[0].Version);
            Assert.Equal("ACTION", result[0].Action);

            await nuget.Received(1).GetDataAsync(
                Arg.Is<PackageData>(p => p.Id == "Serilog" && p.Version == "3.0.0"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldSkipPackage_WhenVersionRemainsUnresolved()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Serilog" />
              </ItemGroup>
            </Project>
            """;

            var project = CreateProjectWithContent("/src/App/App.csproj", projectContent);

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Empty(result);
            await nuget.DidNotReceive().GetDataAsync(Arg.Any<PackageData>(), Arg.Any<CancellationToken>());
            rec.DidNotReceive().DetermineAction(Arg.Any<PackageData>());
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldMatchCaseInsensitiveAndQuestionMarkWildcard_AndIgnoreBlankPatterns()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                // Lowercase pattern to validate case-insensitive matching, and '*' wildcard
                SkipInternalPackagesFilter = new List<string> { "  ", "emerson.uti*  " },
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Util", "1.2.3"), ("Emerson.Utility", "2.0.0"), ("Other", "1.0.0"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

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
            var npm = Substitute.For<INpmMetadataService>();
            var rec = Substitute.For<IPackageRecommendationService>();
            var resolverLogger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(resolverLogger);
            var svc = new ProjectAnalysisService(logger, nuget, npm, rec, resolver);
            return (svc, nuget, rec, logger);
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldSkipInternalPackagesUsingPatterns_AndSetSkipMessage()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = new List<string> { "Emerson.*" },
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };

            // For non-internal packages, DetermineAction returns a value
            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"), ("Newtonsoft.Json", "13.0.1"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            var internalPkg = result.Find(p => p.Id == "Emerson.Core");
            Assert.NotNull(internalPkg);
            Assert.Equal("INTERNAL", internalPkg!.Action);

            await nuget.DidNotReceive().GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"), Arg.Any<CancellationToken>());
            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"), Arg.Any<CancellationToken>());

            rec.Received(1).Initialize(packageConfig);
            rec.Received(1).DetermineAction(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"));
            rec.DidNotReceive().DetermineAction(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"));
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldNotSkip_WhenNoPatternsProvided()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = new List<string>(),
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };

            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"), ("Newtonsoft.Json", "13.0.1"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Emerson.Core"), Arg.Any<CancellationToken>());
            await nuget.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "Newtonsoft.Json"), Arg.Any<CancellationToken>());

            rec.Received(1).Initialize(packageConfig);
            rec.Received(2).DetermineAction(Arg.Any<PackageData>());

            // None should be marked with INTERNAL skip message
            Assert.DoesNotContain(result, p => p.Action == "INTERNAL");
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldNotSkip_WhenPatternsAreNull()
        {
            // Arrange
            var (svc, nuget, rec, _) = CreateService();
            var packageConfig = new PackageRecommendationConfig
            {
                SkipInternalPackagesFilter = null!,
                Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = packageConfig
            };
            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = CreateProjectWithPackages(("Emerson.Core", "1.0.0"));

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("Emerson.Core", result[0].Id);
            Assert.Equal("ACTION", result[0].Action);
            await nuget.Received(1).GetDataAsync(Arg.Any<PackageData>(), Arg.Any<CancellationToken>());
            rec.Received(1).DetermineAction(Arg.Any<PackageData>());
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldAnalyzeNpmProject_WhenProjectTypeIsNpm()
        {
            // Arrange
            var (svc, nuget, npm, rec, _) = CreateServiceWithMocks();
            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = new PackageRecommendationConfig
                {
                    Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
                }
            };
            rec.DetermineAction(Arg.Any<PackageData>()).Returns("ACTION");

            var project = new ProjectInfo
            {
                Name = "NpmProject",
                FilePath = "package.json",
                RepositoryName = "Repo",
                ProjectType = ProjectType.Npm,
                Content = """
                {
                  "dependencies": {
                    "lodash": "4.17.21"
                  }
                }
                """
            };

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles { IncludeNpmDevDependencies = false }, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("lodash", result[0].Id);
            Assert.Equal("ACTION", result[0].Action);
            await npm.Received(1).GetDataAsync(Arg.Is<PackageData>(p => p.Id == "lodash"), Arg.Any<CancellationToken>());
            await nuget.DidNotReceive().GetDataAsync(Arg.Any<PackageData>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldThrowNotSupportedException_WhenProjectTypeIsUnsupported()
        {
            // Arrange
            var (svc, _, _, _, _) = CreateServiceWithMocks();
            var project = new ProjectInfo
            {
                Name = "Unknown",
                FilePath = "unknown.file",
                RepositoryName = "Repo",
                Content = "{}",
                ProjectType = (ProjectType)999
            };

            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = new PackageRecommendationConfig
                {
                    Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
                }
            };

            // Act / Assert
            await Assert.ThrowsAsync<NotSupportedException>(() =>
                svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeProjectAsync_ShouldReturnEmpty_WhenCSharpResolverThrows()
        {
            // Arrange
            var logger = Substitute.For<ILogger<ProjectAnalysisService>>();
            var nuget = Substitute.For<INugetMetadataService>();
            var npm = Substitute.For<INpmMetadataService>();
            var rec = Substitute.For<IPackageRecommendationService>();
            var resolver = Substitute.For<ICSharpPackageVersionResolver>();
            resolver.ResolvePackageVersions(Arg.Any<ProjectInfo>())
                .Returns(_ => throw new InvalidOperationException("resolver failed"));

            var svc = new ProjectAnalysisService(logger, nuget, npm, rec, resolver);
            var project = CreateProjectWithPackages(("Serilog", "3.0.0"));
            var config = new EOLAnalysisConfig
            {
                PackageRecommendation = new PackageRecommendationConfig
                {
                    Messages = new PackageActionMessages { SkipInternal = "INTERNAL" }
                }
            };

            // Act
            var result = await svc.AnalyzeProjectAsync(project, config, new FeatureToggles(), CancellationToken.None);

            // Assert
            Assert.Empty(result);
            await nuget.DidNotReceive().GetDataAsync(Arg.Any<PackageData>(), Arg.Any<CancellationToken>());
            rec.DidNotReceive().DetermineAction(Arg.Any<PackageData>());
        }

        private static (ProjectAnalysisService svc, INugetMetadataService nuget, INpmMetadataService npm, IPackageRecommendationService rec, ILogger<ProjectAnalysisService> logger)
            CreateServiceWithMocks()
        {
            var logger = Substitute.For<ILogger<ProjectAnalysisService>>();
            var nuget = Substitute.For<INugetMetadataService>();
            var npm = Substitute.For<INpmMetadataService>();
            var rec = Substitute.For<IPackageRecommendationService>();
            var resolverLogger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(resolverLogger);
            var svc = new ProjectAnalysisService(logger, nuget, npm, rec, resolver);
            return (svc, nuget, npm, rec, logger);
        }
    }
}
