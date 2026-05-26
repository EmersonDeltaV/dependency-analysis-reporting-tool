namespace DART.Runtime;

public interface IDartExecutionRunner
{
    Task<DartExecutionResult> RunAsync(
        DartExecutionRequest request,
        IProgress<DartExecutionProgress>? progress,
        CancellationToken cancellationToken);
}