using DART.EOLAnalysis.Helpers;
using DART.EOLAnalysis.Models;
using DART.EOLAnalysis.Services;

namespace DART.Tests.DART.EOLAnalysis.Services
{
    public class NpmMetadataServiceTests
    {
        [Theory]
        [InlineData("^1.2.3", "1.2.3")]
        [InlineData("~2.0.0", "2.0.0")]
        [InlineData(">=3.1.0", "3.1.0")]
        [InlineData(">4.0.0", "4.0.0")]
        [InlineData("<=5.0.0", "5.0.0")]
        [InlineData("<6.0.0", "6.0.0")]
        [InlineData("=7.0.0", "7.0.0")]
        [InlineData("8.0.0", "8.0.0")]
        [InlineData("  ^1.0.0  ", "1.0.0")]
        public void StripSemverPrefix_RemovesPrefixCorrectly(string input, string expected)
        {
            var result = PackageJsonHelper.StripSemverPrefix(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void StripSemverPrefix_EmptyString_ReturnsEmpty()
        {
            var result = PackageJsonHelper.StripSemverPrefix(string.Empty);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Initialize_NullOrWhitespace_ThrowsArgumentException()
        {
            var svc = new NpmMetadataService();
            Assert.Throws<ArgumentException>(() => svc.Initialize(string.Empty));
            Assert.Throws<ArgumentException>(() => svc.Initialize("   "));
        }

        [Fact]
        public async Task GetDataAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            var svc = new NpmMetadataService();
            var data = new PackageData { Id = "lodash", Version = "4.17.21" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetDataAsync(data));
        }
    }
}
