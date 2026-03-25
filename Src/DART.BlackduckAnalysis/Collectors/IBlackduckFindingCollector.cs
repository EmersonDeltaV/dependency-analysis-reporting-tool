namespace DART.BlackduckAnalysis;

public interface IBlackduckFindingCollector
{
    Task<IReadOnlyList<BlackduckCollectedFinding>> CollectAsync(
        IReadOnlyCollection<BlackduckRawFinding> rows,
        BlackduckCollectorOptions options,
        Func<string, CancellationToken, Task<string>> recommendedFixResolver,
        CancellationToken cancellationToken = default);
}
