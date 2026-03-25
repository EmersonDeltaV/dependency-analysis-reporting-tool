namespace DART.Core.Contracts;

public sealed class DartRunIssue
{
    public string Source { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public bool IsWarning { get; init; } = true;
}
