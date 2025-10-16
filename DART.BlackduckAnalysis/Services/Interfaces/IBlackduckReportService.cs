namespace DART.BlackduckAnalysis
{
    public interface IBlackduckReportService
    {
        Task<string> DownloadVulnerabilityReport();
    }
}