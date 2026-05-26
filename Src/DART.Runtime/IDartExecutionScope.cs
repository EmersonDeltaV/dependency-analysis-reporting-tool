namespace DART.Runtime;

public interface IDartExecutionScope : IDisposable, IAsyncDisposable
{
    IServiceProvider Services { get; }
}