using DART.Core;

namespace DART.Tests.DART.Core.Contracts;

public class EolFindingTests
{
    [Fact]
    public void EolFinding_ShouldBeConstructible()
    {
        var finding = new EolFinding();

        Assert.NotNull(finding);
    }
}


