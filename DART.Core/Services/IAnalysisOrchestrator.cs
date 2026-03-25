using DART.Core.Contracts;

namespace DART.Core.Services;

public interface IAnalysisOrchestrator
{
    Task<AnalysisResult> RunAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

