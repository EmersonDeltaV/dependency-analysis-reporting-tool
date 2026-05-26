using DART.Core;

namespace DART.Runtime;

public sealed class DartExecutionResult
{
    public RunStatus Status { get; init; } = RunStatus.NotStarted;

    public IReadOnlyList<BlackduckFinding> BlackduckFindings { get; init; } = Array.Empty<BlackduckFinding>();

    public IReadOnlyList<EolFinding> EolFindings { get; init; } = Array.Empty<EolFinding>();

    public IReadOnlyList<RunIssue> Issues { get; init; } = Array.Empty<RunIssue>();

    public string? ReportPath { get; init; }
}