public interface IBlackduckApiService
{
    Task<bool> CreateVulnerabilityStatusReport();

    Task<string> GetLatestVulnerabilityReportId();

    Task<bool> GetVulnerabilityStatusReportCompleteStatus(string reportId);

    Task<string> SaveReport(string reportId);

    Task<string> GetRecommendedFix(string CVECode);

}