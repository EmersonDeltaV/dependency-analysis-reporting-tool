namespace DART.Core.Contracts;

public sealed class DartAnalysisResult
{
    public List<DartBlackduckFinding> BlackduckFindings { get; init; } = new();

    public List<DartEolFinding> EolFindings { get; init; } = new();

    public List<DartRunIssue> Issues { get; init; } = new();

    public DartRunStatus Status { get; set; } = DartRunStatus.NotStarted;
}
