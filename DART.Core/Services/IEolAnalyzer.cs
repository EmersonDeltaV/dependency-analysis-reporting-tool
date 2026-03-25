using DART.Core.Contracts;

namespace DART.Core.Services;

public interface IEolAnalyzer
{
    Task<IReadOnlyCollection<EolFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

