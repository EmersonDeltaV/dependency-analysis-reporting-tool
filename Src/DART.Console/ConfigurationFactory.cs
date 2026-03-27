using Microsoft.Extensions.Configuration;

namespace DART.Console
{
    public static class ConfigurationFactory
    {
        public static IConfiguration BuildConfiguration(string basePath)
        {
            var appCode = Environment.GetEnvironmentVariable("DART_APP_CODE");

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json");

            if (!string.IsNullOrWhiteSpace(appCode))
            {
                configurationBuilder.AddJsonFile($"config.{appCode}.json", optional: true);
            }

            return configurationBuilder
                .AddEnvironmentVariables(prefix: "DART_")
                .Build();
        }
    }
}
