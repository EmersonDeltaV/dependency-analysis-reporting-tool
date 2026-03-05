namespace DART.BlackduckAnalysis
{
    public interface IBlackduckApiService
    {
        Task<bool> CreateVulnerabilityStatusReport(BlackduckConfiguration config);

        Task<string> GetLatestVulnerabilityReportId(BlackduckConfiguration config);

        Task<bool> GetVulnerabilityStatusReportStatus(BlackduckConfiguration config, string reportId);

        Task<string> SaveReport(BlackduckConfiguration config, string reportId, string reportFolderPath);

        Task<string> GetRecommendedFix(BlackduckConfiguration config, string CVECode);

        Task<Dictionary<string, string>> GetLatestProjectVersion(BlackduckConfiguration config);
    }
}
