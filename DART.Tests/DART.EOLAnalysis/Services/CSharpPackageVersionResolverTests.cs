using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DART.Tests.DART.EOLAnalysis.Services
{
    public class CSharpPackageVersionResolverTests
    {
        [Fact]
        public void ResolvePackageVersions_ShouldPreferDirectVersion_WhenCpmAlsoDefinesPackage()
        {
            var logger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(logger);

            var project = CreateProjectInfo(
                "/src/App/App.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" Version="1.2.3" />
                  </ItemGroup>
                </Project>
                """,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/src/Directory.Packages.props"] = """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Serilog" Version="9.9.9" />
                      </ItemGroup>
                    </Project>
                    """
                });

            var result = resolver.ResolvePackageVersions(project);

            var package = Assert.Single(result);
            Assert.Equal("Serilog", package.Id);
            Assert.Equal("1.2.3", package.Version);
        }

        [Fact]
        public void ResolvePackageVersions_ShouldUseNearestPropsFile_WhenMultipleScopesExist()
        {
            var logger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(logger);

            var project = CreateProjectInfo(
                "/src/App/Sub/App.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" />
                  </ItemGroup>
                </Project>
                """,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/src/Directory.Packages.props"] = """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Serilog" Version="1.0.0" />
                      </ItemGroup>
                    </Project>
                    """,
                    ["/src/App/Directory.Packages.props"] = """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Serilog" Version="2.0.0" />
                      </ItemGroup>
                    </Project>
                    """
                });

            var result = resolver.ResolvePackageVersions(project);

            var package = Assert.Single(result);
            Assert.Equal("Serilog", package.Id);
            Assert.Equal("2.0.0", package.Version);
        }

        [Fact]
        public void ResolvePackageVersions_ShouldResolveVersionFromImportedParentProps()
        {
            var logger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(logger);

            var project = CreateProjectInfo(
                "/src/App/App.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" />
                  </ItemGroup>
                </Project>
                """,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/src/App/Directory.Packages.props"] = """
                    <Project>
                      <Import Project="$(MSBuildThisFileDirectory)..\Directory.Packages.props" />
                    </Project>
                    """,
                    ["/src/Directory.Packages.props"] = """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Serilog" Version="3.0.0" />
                      </ItemGroup>
                    </Project>
                    """
                });

            var result = resolver.ResolvePackageVersions(project);

            var package = Assert.Single(result);
            Assert.Equal("Serilog", package.Id);
            Assert.Equal("3.0.0", package.Version);
        }

        [Fact]
        public void ResolvePackageVersions_ShouldDisableCpmFallback_WhenManagePackageVersionsCentrallyIsFalse()
        {
            var logger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(logger);

            var project = CreateProjectInfo(
                "/src/App/App.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Serilog" />
                  </ItemGroup>
                </Project>
                """,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["/src/Directory.Packages.props"] = """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Serilog" Version="4.0.0" />
                      </ItemGroup>
                    </Project>
                    """
                });

            var result = resolver.ResolvePackageVersions(project);

            Assert.Empty(result);
        }

        [Fact]
        public void ResolvePackageVersions_ShouldWarnAndSkip_WhenVersionCannotBeResolved()
        {
            var logger = Substitute.For<ILogger<CSharpPackageVersionResolver>>();
            var resolver = new CSharpPackageVersionResolver(logger);

            var project = CreateProjectInfo(
                "/src/App/App.csproj",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Serilog" />
                  </ItemGroup>
                </Project>
                """);

            var result = resolver.ResolvePackageVersions(project);

            Assert.Empty(result);
            Assert.True(HasLog(logger, LogLevel.Warning, "Could not resolve version for package Serilog"));
        }

        private static ProjectInfo CreateProjectInfo(
            string filePath,
            string content,
            IReadOnlyDictionary<string, string>? propsByPath = null)
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

        private static bool HasLog(ILogger<CSharpPackageVersionResolver> logger, LogLevel level, string messageFragment)
        {
            return logger.ReceivedCalls().Any(call =>
            {
                if (!string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal))
                {
                    return false;
                }

                var args = call.GetArguments();
                if (args.Length < 3 || args[0] is not LogLevel logLevel || logLevel != level)
                {
                    return false;
                }

                var stateMessage = args[2]?.ToString();
                return stateMessage?.Contains(messageFragment, StringComparison.OrdinalIgnoreCase) == true;
            });
        }
    }
}
