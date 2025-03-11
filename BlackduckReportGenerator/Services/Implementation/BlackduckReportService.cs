using BlackduckReportGeneratorTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BlackduckReportGeneratorTool.Services.Implementation
{
    public class BlackduckReportService(IBlackduckApiService blackduckApiService,
    IConfiguration configuration) : IBlackduckReportService
    {
        private const string KEY_MAX_TRIES = "DownloadConfiguration:DownloadParameters:MaxTries";

        private readonly IBlackduckApiService blackduckApiService = blackduckApiService;
        private readonly IConfiguration configuration = configuration;

        public async Task<string> DownloadVulnerabilityReport()
        {
            // Attempt to create a vulnerability status report
            var isCreateSuccess = await blackduckApiService.CreateVulnerabilityStatusReport();

            // If report creation fails, return an empty string
            if (!isCreateSuccess)
            {
                return string.Empty;
            }

            var reportId = string.Empty;
            int tryCount = 0;

            // Get the maximum number of tries from the configuration
            var maxTries = configuration.GetSection(KEY_MAX_TRIES).Get<int>();

            // Try to get the latest vulnerability report ID until successful or max tries reached
            while (reportId == string.Empty && tryCount < maxTries)
            {
                // Wait for 20 seconds before each retry
                await Task.Delay(20000);
                reportId = await blackduckApiService.GetLatestVulnerabilityStatusReportId();
                tryCount++;
            }

            // If no report ID is obtained, return an empty string
            if (reportId == string.Empty)
            {
                return string.Empty;
            }

            // Download the report asynchronously and wait for completion
            string reportPath = await blackduckApiService.SaveReport(reportId);

            // Return the report path
            return reportPath;
        }
    }
}