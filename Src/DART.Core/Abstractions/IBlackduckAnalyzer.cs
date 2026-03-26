namespace DART.Core;

public interface IBlackduckAnalyzer
{
    Task<IReadOnlyCollection<BlackduckFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

