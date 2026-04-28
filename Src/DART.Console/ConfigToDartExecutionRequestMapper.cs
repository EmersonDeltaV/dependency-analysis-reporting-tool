using DART.Runtime;

namespace DART.Console;

public static class ConfigToDartExecutionRequestMapper
{
    public static DartExecutionRequest Map(Config config)
    {
        return new DartExecutionRequest
        {
            AppCode = Environment.GetEnvironmentVariable("DART_APP_CODE") ?? "app",
            ReportConfiguration = config.ReportConfiguration,
            BlackduckConfiguration = config.BlackduckConfiguration,
            EolAnalysisConfiguration = config.EOLAnalysis,
            FeatureToggles = config.FeatureToggles
        };
    }
}