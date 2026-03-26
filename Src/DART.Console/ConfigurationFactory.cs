using Microsoft.Extensions.Configuration;

namespace DART.Console
{
    public static class ConfigurationFactory
    {
        public static IConfiguration BuildConfiguration(string basePath)
        {
            var appCode = Environment.GetEnvironmentVariable("DART_APP_CODE");

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json")
                .AddJsonFile($"config.{appCode}.json", optional: true)
                .AddEnvironmentVariables(prefix: "DART_")
                .Build();
        }
    }
}
