using DART.Core;

namespace DART.Tests.DART.Core.Configuration;

public class ConfigPathResolutionTests
{
    [Fact]
    public void OutputFilePath_ShouldResolveRelativePathAgainstCurrentDirectory()
    {
        var config = new ReportConfiguration { OutputFilePath = "BlackDuck" };

        Assert.True(Path.IsPathRooted(config.OutputFilePath));
        Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "BlackDuck"), config.OutputFilePath);
    }

    [Fact]
    public void LogPath_ShouldResolveRelativePathAgainstCurrentDirectory()
    {
        var config = new ReportConfiguration { LogPath = "Logs" };

        Assert.True(Path.IsPathRooted(config.LogPath));
        Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "Logs"), config.LogPath);
    }

    [Fact]
    public void OutputFilePath_ShouldKeepAbsolutePathUnchanged()
    {
        var absolutePath = Path.GetFullPath(Path.Combine("C:\\", "BlackDuck"));
        var config = new ReportConfiguration { OutputFilePath = absolutePath };

        Assert.Equal(absolutePath, config.OutputFilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OutputFilePath_ShouldReturnEmpty_WhenUnsetOrWhitespace(string configuredValue)
    {
        var config = new ReportConfiguration { OutputFilePath = configuredValue };

        Assert.Equal(string.Empty, config.OutputFilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LogPath_ShouldReturnEmpty_WhenUnsetOrWhitespace(string configuredValue)
    {
        var config = new ReportConfiguration { LogPath = configuredValue };

        Assert.Equal(string.Empty, config.LogPath);
    }
}
