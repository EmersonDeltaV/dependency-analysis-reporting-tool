using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class BlackduckFindingTests
{
    [Fact]
    public void BlackduckFinding_ShouldBeConstructible()
    {
        var finding = new BlackduckFinding();

        Assert.NotNull(finding);
    }
}


