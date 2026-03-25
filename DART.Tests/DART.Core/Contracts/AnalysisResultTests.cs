using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class AnalysisResultTests
{
    [Fact]
    public void AnalysisResult_ShouldInitializeCollections()
    {
        var result = new AnalysisResult();

        Assert.NotNull(result.BlackduckFindings);
        Assert.NotNull(result.EolFindings);
        Assert.NotNull(result.Issues);
    }

    [Fact]
    public void AnalysisResult_ShouldInitializeStatusToNotStarted()
    {
        var result = new AnalysisResult();

        Assert.Equal(RunStatus.NotStarted, result.Status);
    }
}


