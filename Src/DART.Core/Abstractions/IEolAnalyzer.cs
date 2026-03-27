namespace DART.Core;

public interface IEolAnalyzer
{
    Task<IReadOnlyCollection<EolFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}
