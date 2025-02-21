namespace BlackduckReportGeneratorTool.Services.Interfaces
{
    public interface IBlackduckReportService
    {
        Task<string> DownloadVulnerabilityReport();
    }
}