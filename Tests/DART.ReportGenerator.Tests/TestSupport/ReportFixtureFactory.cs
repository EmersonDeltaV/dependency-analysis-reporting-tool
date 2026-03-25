using DART.Core;

namespace DART.Tests.DART.ReportGenerator.TestSupport;

internal static class ReportFixtureFactory
{
    public static List<RowDetails> CreateSampleRows() =>
    [
        new RowDetails
        {
            ApplicationName = "APP01",
            SoftwareComponent = "Newtonsoft.Json",
            Version = "13.0.3",
            SecurityRisk = "HIGH",
            VulnerabilityId = "CVE-2024-10001",
            RecommendedFix = "13.0.4",
            MatchType = "Direct Dependency"
        },
        new RowDetails
        {
            ApplicationName = "APP01",
            SoftwareComponent = "CsvHelper",
            Version = "30.0.1",
            SecurityRisk = "MEDIUM",
            VulnerabilityId = "CVE-2024-10002",
            RecommendedFix = "30.0.2",
            MatchType = "Transitive Dependency"
        }
    ];
}
