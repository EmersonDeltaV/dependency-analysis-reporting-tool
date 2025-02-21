public interface IBlackduckApiService
{
    Task<bool> CreateVulnerabilityStatusReport();

    Task<string> GetLatestVulnerabilityStatusReportId();

    Task<string> SaveReport(string reportId);

    Task<string> GetRecommendedFix(string CVECode);

}