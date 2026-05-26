namespace DART.Runtime;

public interface IDartExecutionScopeFactory
{
    IDartExecutionScope Create(DartExecutionRequest request);
}