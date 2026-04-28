using Microsoft.Extensions.DependencyInjection;

namespace DART.Runtime.Tests.Runtime;

[Trait("Category", "Unit")]
public sealed class DartRuntimeServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDartRuntime_ShouldRegisterScopeFactory()
    {
        var services = new ServiceCollection();

        services.AddDartRuntime();

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        var scopeFactory = serviceProvider.GetRequiredService<IDartExecutionScopeFactory>();

        Assert.IsType<ServiceProviderDartExecutionScopeFactory>(scopeFactory);
    }
}