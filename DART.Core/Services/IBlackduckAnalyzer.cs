using DART.Core.Contracts;

namespace DART.Core.Services;

public interface IBlackduckAnalyzer
{
    Task<IReadOnlyCollection<BlackduckFinding>> AnalyzeAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

