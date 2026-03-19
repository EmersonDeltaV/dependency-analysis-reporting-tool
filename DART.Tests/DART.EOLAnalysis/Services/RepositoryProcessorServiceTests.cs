using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DART.Tests.DART.EOLAnalysis.Services
{
    public class RepositoryProcessorServiceTests
    {
        [Fact]
        public async Task ProcessRepositoryAsync_ShouldReturnCSharpAndNpmProjects_WithExpectedContexts()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = CreateRepository();
            var config = CreateConfig();
            var toggles = new FeatureToggles { EnableCSharpAnalysis = true, EnableNpmAnalysis = true };

            client.FindDirectoryPackagesPropsFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/src/Directory.Packages.props" },
                    new GitItem { Path = "/src/Empty/Directory.Packages.props" }
                ]);
            client.FindCsProjFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/src/App/App.csproj" },
                    new GitItem { Path = "Root.csproj" }
                ]);
            client.FindPackageJsonFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/web/package.json" }
                ]);

            client.GetFileContentAsync(Arg.Any<Repository>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var path = ci.ArgAt<string>(1);
                    return path switch
                    {
                        "/src/Directory.Packages.props" => Task.FromResult("<Project />"),
                        "/src/Empty/Directory.Packages.props" => Task.FromResult(string.Empty),
                        "/src/App/App.csproj" => Task.FromResult("<Project Sdk=\"Microsoft.NET.Sdk\" />"),
                        "Root.csproj" => Task.FromResult("<Project />"),
                        "/web/package.json" => Task.FromResult("{\"dependencies\":{\"lodash\":\"4.17.21\"}}"),
                        _ => Task.FromResult(string.Empty)
                    };
                });

            var result = await sut.ProcessRepositoryAsync(repository, client, config, toggles, CancellationToken.None);

            Assert.Equal(3, result.Count);

            var csharpProjects = result.Where(p => p.ProjectType == ProjectType.CSharp).ToList();
            var npmProjects = result.Where(p => p.ProjectType == ProjectType.Npm).ToList();
            Assert.Equal(2, csharpProjects.Count);
            Assert.Single(npmProjects);

            Assert.Contains(csharpProjects, p => p.Name == "App" && p.FilePath == "/src/App/App.csproj");
            Assert.Contains(csharpProjects, p => p.Name == repository.Name && p.FilePath == "Root.csproj");
            Assert.Equal("repo-name", csharpProjects[0].RepositoryName);
            Assert.Equal("repo-name", npmProjects[0].RepositoryName);

            Assert.All(csharpProjects, p =>
            {
                Assert.Single(p.DirectoryPackagesPropsByPath);
                Assert.Contains("/src/Directory.Packages.props", p.DirectoryPackagesPropsByPath.Keys);
            });
            Assert.Empty(npmProjects[0].DirectoryPackagesPropsByPath);
        }

        [Fact]
        public async Task ProcessRepositoryAsync_ShouldReturnEmpty_WhenCSharpEnabledAndNoPropsOrCsprojFiles()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = CreateRepository();
            var config = CreateConfig();
            var toggles = new FeatureToggles { EnableCSharpAnalysis = true, EnableNpmAnalysis = false };

            client.FindDirectoryPackagesPropsFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns(new List<GitItem>());
            client.FindCsProjFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns(new List<GitItem>());

            var result = await sut.ProcessRepositoryAsync(repository, client, config, toggles, CancellationToken.None);

            Assert.Empty(result);
            await client.DidNotReceive().FindPackageJsonFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRepositoryAsync_ShouldReturnEmpty_WhenNpmEnabledAndFindReturnsNull()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = CreateRepository();
            var config = CreateConfig();
            var toggles = new FeatureToggles { EnableCSharpAnalysis = false, EnableNpmAnalysis = true };

            client.FindPackageJsonFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns((List<GitItem>)null!);

            var result = await sut.ProcessRepositoryAsync(repository, client, config, toggles, CancellationToken.None);

            Assert.Empty(result);
            await client.DidNotReceive().FindCsProjFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRepositoryAsync_ShouldSkipProject_WhenGetFileContentThrows()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = CreateRepository();
            var config = CreateConfig();
            var toggles = new FeatureToggles { EnableCSharpAnalysis = true, EnableNpmAnalysis = false };

            client.FindDirectoryPackagesPropsFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns(new List<GitItem>());
            client.FindCsProjFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/src/Fail/Fail.csproj" },
                    new GitItem { Path = "/src/Ok/Ok.csproj" }
                ]);

            client.GetFileContentAsync(Arg.Any<Repository>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var path = ci.ArgAt<string>(1);
                    if (path == "/src/Fail/Fail.csproj")
                    {
                        throw new InvalidOperationException("simulated failure");
                    }

                    return Task.FromResult("<Project />");
                });

            var result = await sut.ProcessRepositoryAsync(repository, client, config, toggles, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal("/src/Ok/Ok.csproj", result[0].FilePath);
            Assert.Equal("Ok", result[0].Name);
        }

        [Fact]
        public async Task ProcessRepositoryAsync_ShouldContinue_WhenPropsContentIsEmptyOrThrows()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = CreateRepository();
            var config = CreateConfig();
            var toggles = new FeatureToggles { EnableCSharpAnalysis = true, EnableNpmAnalysis = false };

            client.FindDirectoryPackagesPropsFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/src/Empty/Directory.Packages.props" },
                    new GitItem { Path = "/src/Broken/Directory.Packages.props" }
                ]);
            client.FindCsProjFilesAsync(Arg.Any<Repository>(), Arg.Any<CancellationToken>())
                .Returns([
                    new GitItem { Path = "/src/App/App.csproj" }
                ]);

            client.GetFileContentAsync(Arg.Any<Repository>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var path = ci.ArgAt<string>(1);
                    if (path == "/src/Broken/Directory.Packages.props")
                    {
                        throw new InvalidOperationException("cannot load props");
                    }

                    return path switch
                    {
                        "/src/Empty/Directory.Packages.props" => Task.FromResult("   "),
                        "/src/App/App.csproj" => Task.FromResult("<Project />"),
                        _ => Task.FromResult(string.Empty)
                    };
                });

            var result = await sut.ProcessRepositoryAsync(repository, client, config, toggles, CancellationToken.None);

            Assert.Single(result);
            Assert.Empty(result[0].DirectoryPackagesPropsByPath);
            Assert.Equal("/src/App/App.csproj", result[0].FilePath);
        }

        [Fact]
        public async Task ProcessRepositoryAsync_ShouldRethrow_WhenRepositoryUrlIsInvalid()
        {
            var sut = CreateService();
            var client = Substitute.For<IAzureDevOpsClient>();
            var repository = new Repository
            {
                Name = "InvalidRepo",
                Url = "not-a-valid-url"
            };

            await Assert.ThrowsAnyAsync<Exception>(() =>
                sut.ProcessRepositoryAsync(repository, client, CreateConfig(), new FeatureToggles(), CancellationToken.None));
        }

        private static RepositoryProcessorService CreateService()
        {
            var logger = Substitute.For<ILogger<RepositoryProcessorService>>();
            return new RepositoryProcessorService(logger);
        }

        private static Repository CreateRepository()
        {
            return new Repository
            {
                Name = "SampleRepo",
                Url = "https://dev.azure.com/org-name/project-name/_git/repo-name",
                Branch = "main"
            };
        }

        private static EOLAnalysisConfig CreateConfig()
        {
            return new EOLAnalysisConfig
            {
                MaxConcurrency = 2,
                BoundedCapacity = 1
            };
        }
    }
}
