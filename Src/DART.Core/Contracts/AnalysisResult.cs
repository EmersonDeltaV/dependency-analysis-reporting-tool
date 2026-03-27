namespace DART.Core;

public sealed class AnalysisResult
{
    public List<BlackduckFinding> BlackduckFindings { get; init; } = new();

    public List<EolFinding> EolFindings { get; init; } = new();

    public List<RunIssue> Issues { get; init; } = new();

    public RunStatus Status { get; set; } = RunStatus.NotStarted;
}

