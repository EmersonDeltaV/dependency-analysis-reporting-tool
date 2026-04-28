using Microsoft.Extensions.DependencyInjection;

namespace DART.Runtime.Tests.Runtime;

[Trait("Category", "Unit")]
public sealed class DartRuntimeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDartRuntime_ShouldRegisterRuntimeServices()
    {
        var services = new ServiceCollection();

        services.AddDartRuntime();

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var scopeFactory = serviceProvider.GetRequiredService<IDartExecutionScopeFactory>();
        var executionRunner = serviceProvider.GetRequiredService<IDartExecutionRunner>();

        Assert.IsType<ServiceProviderDartExecutionScopeFactory>(scopeFactory);
        Assert.IsType<DartExecutionRunner>(executionRunner);
    }
}