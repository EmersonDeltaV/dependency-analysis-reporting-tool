using DART.BlackduckAnalysis;

namespace DART.Tests.DART.BlackduckAnalysis.Helpers
{
    public class RecommendedFixFormatterTests
    {
        [Theory]
        [InlineData(
            "Fixed in 1.12.0 by this commit.The latest stable releases are available here.",
            "Fixed in 1.12.0")]
        [InlineData(
            "Fixed in 3.4.5 by this commit. The latest stable releases are available here.",
            "Fixed in 3.4.5")]
        [InlineData(
            "* Fixed in (ref) 7.8.9 by this commit\nThe latest stable releases are available here.",
            "Fixed in 7.8.9")]
        public void Format_Should_StripCommitAndLatestStable_ForSingleVersion(string input, string expected)
        {
            var actual = RecommendedFixFormatter.Format(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(
            "Fixed in version: 4.0.4 by this commit. 3.0.4 by this commit. 2.5.4 by this commit.The latest stable releases can be found here.",
            "Fixed in 4.0.4, 3.0.4, 2.5.4")]
        [InlineData(
            "Fixed in version: (x) 1.0.0 by this commit. (ref) 1.0.1 by this commit. The latest stable releases are available here.",
            "Fixed in 1.0.0, 1.0.1")]
        public void Format_Should_CombineMultipleVersions_WithFixedInPrefix(string input, string expected)
        {
            var actual = RecommendedFixFormatter.Format(input);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(
            "fixed in 8.9.10 BY THIS COMMIT.The LATEST stable releases can be found here.",
            "Fixed in 8.9.10")]
        [InlineData(
            "Fixed in 1.2.3 by this commit.The Latest Stable Releases are Available Here.",
            "Fixed in 1.2.3")]
        public void Format_Should_Handle_MissingSpaces_And_CaseInsensitivity(string input, string expected)
        {
            var actual = RecommendedFixFormatter.Format(input);
            Assert.Equal(expected, actual);
        }
    }
}
