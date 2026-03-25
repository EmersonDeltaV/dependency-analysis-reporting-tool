using DART.BlackduckAnalysis;
using DART.EOLAnalysis;

namespace DART.Core;

public sealed class Config
{
    public ReportConfiguration ReportConfiguration { get; set; } = new();

    public BlackduckConfiguration BlackduckConfiguration { get; set; } = new();

    public FeatureToggles FeatureToggles { get; set; } = new();

    public EOLAnalysisConfig EOLAnalysis { get; set; } = new();
}
