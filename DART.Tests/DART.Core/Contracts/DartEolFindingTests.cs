using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class DartEolFindingTests
{
    [Fact]
    public void DartEolFinding_ShouldBeConstructible()
    {
        var finding = new DartEolFinding();

        Assert.NotNull(finding);
    }
}
