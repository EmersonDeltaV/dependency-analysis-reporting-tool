namespace DART.Core;

public interface IAnalysisOrchestrator
{
    Task<AnalysisResult> RunAsync(AnalysisRequest request, CancellationToken cancellationToken);
}

