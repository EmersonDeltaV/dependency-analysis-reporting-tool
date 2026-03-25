using DART.Core.Contracts;

namespace DART.Tests.DART.Core.Contracts;

public class RunIssueTests
{
    [Fact]
    public void RunIssue_ShouldBeConstructible()
    {
        var issue = new RunIssue();

        Assert.NotNull(issue);
    }
}


