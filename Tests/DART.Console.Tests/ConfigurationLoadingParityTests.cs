using DART.Console;

namespace DART.Tests.DART.Console;

public class ConfigurationLoadingParityTests
{
    [Fact]
    public void BuildConfiguration_ShouldLoadBaseAndAppSpecificJson_ThenApplyEnvOverrides()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dart-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var previousAppCode = Environment.GetEnvironmentVariable("DART_APP_CODE");
        var previousProductName = Environment.GetEnvironmentVariable("DART_ReportConfiguration__ProductName");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "config.json"), """
            {
              "ReportConfiguration": {
                "ProductName": "base-name",
                "ProductVersion": "1.0.0",
                "ProductIteration": "base-iteration"
              },
              "BlackduckConfiguration": {
                "Token": "base-token"
              }
            }
            """);

            File.WriteAllText(Path.Combine(tempDir, "config.APP01.json"), """
            {
              "ReportConfiguration": {
                "ProductName": "app-name",
                "ProductVersion": "2.0.0"
              },
              "BlackduckConfiguration": {
                "Token": "app-token"
              }
            }
            """);

            Environment.SetEnvironmentVariable("DART_APP_CODE", "APP01");
            Environment.SetEnvironmentVariable("DART_ReportConfiguration__ProductName", "env-name");

            var configuration = ConfigurationFactory.BuildConfiguration(tempDir);

            Assert.Equal("env-name", configuration["ReportConfiguration:ProductName"]);
            Assert.Equal("2.0.0", configuration["ReportConfiguration:ProductVersion"]);
            Assert.Equal("base-iteration", configuration["ReportConfiguration:ProductIteration"]);
            Assert.Equal("app-token", configuration["BlackduckConfiguration:Token"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DART_APP_CODE", previousAppCode);
            Environment.SetEnvironmentVariable("DART_ReportConfiguration__ProductName", previousProductName);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildConfiguration_ShouldIgnoreAppSpecificJson_WhenAppCodeIsUnsetOrEmpty(string? appCode)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "dart-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var previousAppCode = Environment.GetEnvironmentVariable("DART_APP_CODE");

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "config.json"), """
            {
              "BlackduckConfiguration": {
                "Token": "base-token"
              }
            }
            """);

            File.WriteAllText(Path.Combine(tempDir, "config..json"), """
            {
              "BlackduckConfiguration": {
                "Token": "unexpected-token"
              }
            }
            """);

            Environment.SetEnvironmentVariable("DART_APP_CODE", appCode);

            var configuration = ConfigurationFactory.BuildConfiguration(tempDir);

            Assert.Equal("base-token", configuration["BlackduckConfiguration:Token"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DART_APP_CODE", previousAppCode);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
