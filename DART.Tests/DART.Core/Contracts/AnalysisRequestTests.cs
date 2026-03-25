using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class AnalysisRequestTests
{
    [Fact]
    public void AnalysisRequest_ShouldBeConstructible()
    {
        var request = new AnalysisRequest();

        Assert.NotNull(request);
    }
}

