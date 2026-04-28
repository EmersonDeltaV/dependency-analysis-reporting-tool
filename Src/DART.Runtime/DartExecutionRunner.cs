using DART.Core;
using DART.ReportGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DART.Runtime;

public sealed class DartExecutionRunner : IDartExecutionRunner
{
    private readonly IDartExecutionScopeFactory _scopeFactory;
    private readonly ILogger<DartExecutionRunner> _logger;

    public DartExecutionRunner(IDartExecutionScopeFactory scopeFactory, ILogger<DartExecutionRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DartExecutionResult> RunAsync(
        DartExecutionRequest request,
        IProgress<DartExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new DartExecutionProgress
        {
            Stage = DartExecutionStage.PreparingRuntime,
            Percent = 10,
            Message = "Preparing runtime"
        });

        await using var scope = _scopeFactory.Create(request);
        var orchestrator = scope.Services.GetRequiredService<IAnalysisOrchestrator>();
        var reportGenerator = scope.Services.GetRequiredService<IReportGenerator>();

        if (!string.IsNullOrWhiteSpace(request.BlackduckConfiguration.PreviousResults)
            && !string.IsNullOrWhiteSpace(request.BlackduckConfiguration.CurrentResults))
        {
            progress?.Report(new DartExecutionProgress
            {
                Stage = DartExecutionStage.ComparingReports,
                Percent = 40,
                Message = "Comparing current and previous reports"
            });

            reportGenerator.CompareCurrentWithPrevious(
                request.BlackduckConfiguration.CurrentResults,
                request.BlackduckConfiguration.PreviousResults);

            progress?.Report(new DartExecutionProgress
            {
                Stage = DartExecutionStage.Completed,
                Percent = 100,
                Message = "Execution completed"
            });

            return new DartExecutionResult
            {
                Status = RunStatus.Completed,
                ReportPath = request.BlackduckConfiguration.CurrentResults
            };
        }

        var analysisRequest = new AnalysisRequest
        {
            EnableBlackduckAnalysis = request.FeatureToggles.EnableBlackduckAnalysis,
            EnableEolAnalysis = request.FeatureToggles.EnableCSharpAnalysis || request.FeatureToggles.EnableNpmAnalysis
        };

        if (analysisRequest.EnableBlackduckAnalysis)
        {
            progress?.Report(new DartExecutionProgress
            {
                Stage = DartExecutionStage.RunningBlackduckAnalysis,
                Percent = 35,
                Message = "Running Black Duck analysis"
            });
        }

        if (analysisRequest.EnableEolAnalysis)
        {
            progress?.Report(new DartExecutionProgress
            {
                Stage = DartExecutionStage.RunningEolAnalysis,
                Percent = analysisRequest.EnableBlackduckAnalysis ? 55 : 45,
                Message = "Running EOL package analysis"
            });
        }

        var analysisResult = await orchestrator.RunAsync(analysisRequest, cancellationToken);

        if (!analysisRequest.EnableBlackduckAnalysis && !analysisRequest.EnableEolAnalysis)
        {
            progress?.Report(new DartExecutionProgress
            {
                Stage = DartExecutionStage.Completed,
                Percent = 100,
                Message = "Execution completed"
            });

            return new DartExecutionResult
            {
                Status = RunStatus.Completed,
                BlackduckFindings = analysisResult.BlackduckFindings,
                EolFindings = analysisResult.EolFindings,
                Issues = analysisResult.Issues
            };
        }

        progress?.Report(new DartExecutionProgress
        {
            Stage = DartExecutionStage.GeneratingWorkbook,
            Percent = 85,
            Message = "Generating workbook"
        });

        var rows = analysisResult.BlackduckFindings.Select(finding => new RowDetails
        {
            ApplicationName = finding.ApplicationName,
            SoftwareComponent = finding.SoftwareComponent,
            Version = finding.Version,
            SecurityRisk = finding.SecurityRisk,
            VulnerabilityId = finding.VulnerabilityId,
            RecommendedFix = finding.RecommendedFix,
            MatchType = finding.MatchType
        }).ToList();

        var reportPath = analysisResult.EolFindings.Count == 0
            ? reportGenerator.GenerateCurrentFormatReport(
                rows,
                request.ReportConfiguration.OutputFilePath,
                request.AppCode,
                request.ReportConfiguration.ProductName,
                request.ReportConfiguration.ProductVersion,
                request.ReportConfiguration.ProductIteration)
            : reportGenerator.GenerateCurrentFormatReport(
                rows,
                analysisResult.EolFindings,
                request.ReportConfiguration.OutputFilePath,
                request.AppCode,
                request.ReportConfiguration.ProductName,
                request.ReportConfiguration.ProductVersion,
                request.ReportConfiguration.ProductIteration);

        if (analysisRequest.EnableBlackduckAnalysis && string.IsNullOrWhiteSpace(request.BlackduckConfiguration.CurrentResults))
        {
            request.BlackduckConfiguration.CurrentResults = reportPath;
        }

        var finalStatus = analysisResult.Status switch
        {
            RunStatus.Completed => RunStatus.Completed,
            RunStatus.CompletedWithWarnings => RunStatus.CompletedWithWarnings,
            RunStatus.Failed => RunStatus.Failed,
            _ => analysisResult.Issues.Count == 0 ? RunStatus.Completed : RunStatus.CompletedWithWarnings
        };

        progress?.Report(new DartExecutionProgress
        {
            Stage = finalStatus switch
            {
                RunStatus.CompletedWithWarnings => DartExecutionStage.CompletedWithWarnings,
                RunStatus.Failed => DartExecutionStage.Failed,
                _ => DartExecutionStage.Completed
            },
            Percent = 100,
            Message = finalStatus switch
            {
                RunStatus.CompletedWithWarnings => "Execution completed with warnings",
                RunStatus.Failed => "Execution failed",
                _ => "Execution completed"
            }
        });

        _logger.LogInformation("DART runtime completed with status {Status} and report {ReportPath}", finalStatus, reportPath);

        return new DartExecutionResult
        {
            Status = finalStatus,
            BlackduckFindings = analysisResult.BlackduckFindings,
            EolFindings = analysisResult.EolFindings,
            Issues = analysisResult.Issues,
            ReportPath = reportPath
        };
    }
}