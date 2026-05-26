namespace DART.Runtime;

public enum DartExecutionStage
{
    Queued = 0,
    PreparingRuntime = 1,
    ComparingReports = 2,
    RunningBlackduckAnalysis = 3,
    RunningEolAnalysis = 4,
    GeneratingWorkbook = 5,
    Completed = 6,
    CompletedWithWarnings = 7,
    Failed = 8
}