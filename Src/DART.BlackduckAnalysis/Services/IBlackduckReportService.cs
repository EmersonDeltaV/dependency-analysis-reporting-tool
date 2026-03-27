namespace DART.BlackduckAnalysis
{
    public interface IBlackduckReportService
    {
        Task<string> DownloadVulnerabilityReport(BlackduckConfiguration config, string reportFolderPath);
    }
}
