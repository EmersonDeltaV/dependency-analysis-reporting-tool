using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class DartAnalysisRequestTests
{
    [Fact]
    public void DartAnalysisRequest_ShouldBeConstructible()
    {
        var request = new DartAnalysisRequest();

        Assert.NotNull(request);
    }
}
