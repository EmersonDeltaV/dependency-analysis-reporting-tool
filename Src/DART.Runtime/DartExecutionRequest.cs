using DART.BlackduckAnalysis;
using DART.Core;
using DART.EOLAnalysis;

namespace DART.Runtime;

public sealed class DartExecutionRequest
{
    public ReportConfiguration ReportConfiguration { get; init; } = new();

    public BlackduckConfiguration BlackduckConfiguration { get; init; } = new();

    public EOLAnalysisConfig EolAnalysisConfiguration { get; init; } = new();

    public FeatureToggles FeatureToggles { get; init; } = new();

    public string AppCode { get; init; } = "app";
}