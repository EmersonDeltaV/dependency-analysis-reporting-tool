using DART.Core.Contracts;

namespace DART.Core.Services;

public sealed class AnalysisOrchestrator : IAnalysisOrchestrator
{
    private readonly IBlackduckAnalyzer _blackduckAnalyzer;
    private readonly IEolAnalyzer _eolAnalyzer;

    public AnalysisOrchestrator(IBlackduckAnalyzer blackduckAnalyzer, IEolAnalyzer eolAnalyzer)
    {
        _blackduckAnalyzer = blackduckAnalyzer;
        _eolAnalyzer = eolAnalyzer;
    }

    public async Task<AnalysisResult> RunAsync(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var result = new AnalysisResult
        {
            Status = RunStatus.Running
        };

        if (request.EnableBlackduckAnalysis)
        {
            try
            {
                var blackduckFindings = await _blackduckAnalyzer.AnalyzeAsync(request, cancellationToken);
                result.BlackduckFindings.AddRange(blackduckFindings);
            }
            catch (Exception ex)
            {
                result.Issues.Add(new RunIssue
                {
                    Source = "Blackduck",
                    Message = ex.Message,
                    IsWarning = true
                });
            }
        }

        if (request.EnableEolAnalysis)
        {
            try
            {
                var eolFindings = await _eolAnalyzer.AnalyzeAsync(request, cancellationToken);
                result.EolFindings.AddRange(eolFindings);
            }
            catch (Exception ex)
            {
                result.Issues.Add(new RunIssue
                {
                    Source = "EOL",
                    Message = ex.Message,
                    IsWarning = true
                });
            }
        }

        result.Status = result.Issues.Count == 0
            ? RunStatus.Completed
            : RunStatus.CompletedWithWarnings;

        return result;
    }
}

