using DART.EOLAnalysis.Helpers;

namespace DART.Tests.DART.EOLAnalysis.Helpers
{
    public class PackageJsonHelperTests
    {
        [Fact]
        public void GetPackagesFromContent_ReturnsProductionAndDevDependencies()
        {
            var json = """
                {
                  "name": "my-app",
                  "dependencies": {
                    "react": "^18.2.0",
                    "axios": "1.6.0"
                  },
                  "devDependencies": {
                    "typescript": "~5.3.3",
                    "jest": "^29.0.0"
                  }
                }
                """;

            var packages = PackageJsonHelper.GetPackagesFromContent(json, includeDevDependencies: true).ToList();

            Assert.Equal(4, packages.Count);
            Assert.Contains(packages, p => p.Name == "react" && p.Version == "^18.2.0");
            Assert.Contains(packages, p => p.Name == "axios" && p.Version == "1.6.0");
            Assert.Contains(packages, p => p.Name == "typescript" && p.Version == "~5.3.3");
            Assert.Contains(packages, p => p.Name == "jest" && p.Version == "^29.0.0");
        }

        [Fact]
        public void GetPackagesFromContent_ReturnsProductionDependencies()
        {
            var json = """
                {
                  "name": "my-app",
                  "dependencies": {
                    "react": "^18.2.0",
                    "axios": "1.6.0"
                  },
                  "devDependencies": {
                    "typescript": "~5.3.3",
                    "jest": "^29.0.0"
                  }
                }
                """;

            var packages = PackageJsonHelper.GetPackagesFromContent(json, includeDevDependencies: false).ToList();

            Assert.Equal(2, packages.Count);
            Assert.Contains(packages, p => p.Name == "react" && p.Version == "^18.2.0");
            Assert.Contains(packages, p => p.Name == "axios" && p.Version == "1.6.0");
            Assert.DoesNotContain(packages, p => p.Name == "typescript");
            Assert.DoesNotContain(packages, p => p.Name == "jest");
        }

        [Fact]
        public void GetPackagesFromContent_OnlyDependencies_ReturnsCorrectCount()
        {
            var json = """
                {
                  "dependencies": {
                    "lodash": "4.17.21"
                  }
                }
                """;

            var packages = PackageJsonHelper.GetPackagesFromContent(json).ToList();

            Assert.Single(packages);
            Assert.Equal("lodash", packages[0].Name);
            Assert.Equal("4.17.21", packages[0].Version);
        }

        [Fact]
        public void GetPackagesFromContent_EmptyDependencies_ReturnsEmpty()
        {
            var json = """{ "name": "empty-app", "dependencies": {}, "devDependencies": {} }""";

            var packages = PackageJsonHelper.GetPackagesFromContent(json).ToList();

            Assert.Empty(packages);
        }

        [Fact]
        public void GetPackagesFromContent_NoDependencySections_ReturnsEmpty()
        {
            var json = """{ "name": "no-deps" }""";

            var packages = PackageJsonHelper.GetPackagesFromContent(json).ToList();

            Assert.Empty(packages);
        }

        [Fact]
        public void GetPackagesFromContent_NullOrEmptyContent_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PackageJsonHelper.GetPackagesFromContent(string.Empty));
            Assert.Throws<ArgumentException>(() => PackageJsonHelper.GetPackagesFromContent("   "));
        }

        [Fact]
        public void GetPackagesFromContent_InvalidJson_ThrowsException()
        {
            Assert.ThrowsAny<System.Text.Json.JsonException>(
                () => PackageJsonHelper.GetPackagesFromContent("not valid json").ToList());
        }
    }
}
