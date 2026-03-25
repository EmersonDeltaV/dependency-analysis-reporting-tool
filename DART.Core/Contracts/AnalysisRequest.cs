namespace DART.Core.Contracts;

public sealed class AnalysisRequest
{
    public bool EnableBlackduckAnalysis { get; init; } = true;

    public bool EnableEolAnalysis { get; init; } = true;
}

