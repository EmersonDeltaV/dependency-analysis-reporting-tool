using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class DartRunIssueTests
{
    [Fact]
    public void DartRunIssue_ShouldBeConstructible()
    {
        var issue = new DartRunIssue();

        Assert.NotNull(issue);
    }
}
