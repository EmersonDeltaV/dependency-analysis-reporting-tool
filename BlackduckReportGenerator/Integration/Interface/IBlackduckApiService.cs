public interface IBlackduckApiService
{
    Task<bool> CreateVulnerabilityStatusReport();

    Task<string> GetLatestVulnerabilityReportId();

    Task<string> DownloadReport(string reportId);

    Task<string> GetRecommendedFix(string CVECode);

}