namespace DART.Runtime;

public sealed class DartExecutionProgress
{
    public DartExecutionStage Stage { get; init; } = DartExecutionStage.Queued;

    public int Percent { get; init; }

    public string Message { get; init; } = string.Empty;
}