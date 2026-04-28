using Microsoft.Extensions.DependencyInjection;

namespace DART.Runtime;

public sealed class ServiceProviderDartExecutionScope : IDartExecutionScope
{
    private readonly ServiceProvider _serviceProvider;

    public ServiceProviderDartExecutionScope(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceProvider Services => _serviceProvider;

    public void Dispose() => _serviceProvider.Dispose();

    public ValueTask DisposeAsync() => _serviceProvider.DisposeAsync();
}