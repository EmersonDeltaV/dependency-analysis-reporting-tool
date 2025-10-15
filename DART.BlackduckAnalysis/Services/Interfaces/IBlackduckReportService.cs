namespace DART.BlackduckAnalysis.Services.Interfaces
{
    public interface IBlackduckReportService
    {
        Task<string> DownloadVulnerabilityReport();
    }
}