namespace DART.Core.Contracts;

public sealed class DartAnalysisRequest
{
    public bool EnableBlackduckAnalysis { get; init; } = true;

    public bool EnableEolAnalysis { get; init; } = true;
}
