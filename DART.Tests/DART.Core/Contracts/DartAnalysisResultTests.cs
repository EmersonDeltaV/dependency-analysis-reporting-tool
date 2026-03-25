using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class DartAnalysisResultTests
{
    [Fact]
    public void DartAnalysisResult_ShouldInitializeCollections()
    {
        var result = new DartAnalysisResult();

        Assert.NotNull(result.BlackduckFindings);
        Assert.NotNull(result.EolFindings);
        Assert.NotNull(result.Issues);
    }

    [Fact]
    public void DartAnalysisResult_ShouldInitializeStatusToNotStarted()
    {
        var result = new DartAnalysisResult();

        Assert.Equal(DartRunStatus.NotStarted, result.Status);
    }
}
