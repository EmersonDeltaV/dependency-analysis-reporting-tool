using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class DartBlackduckFindingTests
{
    [Fact]
    public void DartBlackduckFinding_ShouldBeConstructible()
    {
        var finding = new DartBlackduckFinding();

        Assert.NotNull(finding);
    }
}
