using BlackduckReportGeneratorTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BlackduckReportGeneratorTool.Services.Implementation
{
    public class BlackduckReportService : IBlackduckReportService
    {
        private const string KEY_MAX_TRIES = "DownloadConfiguration:DownloadParameters:MaxTries";

        private readonly IBlackduckApiService blackduckApiService;
        private readonly IConfiguration configuration;

        private readonly int maxNumberofTries;

        public BlackduckReportService(IBlackduckApiService blackduckApiService,
            IConfiguration configuration)
        {
            this.blackduckApiService = blackduckApiService;
            this.configuration = configuration;

            // Get the maximum number of tries from the configuration
            maxNumberofTries = configuration.GetSection(KEY_MAX_TRIES).Get<int>();
        }

        public async Task<string> DownloadVulnerabilityReport()
        {
            // Report Creation: Attempt to create a vulnerability status report
            var isCreateSuccess = await blackduckApiService.CreateVulnerabilityStatusReport();
            // If report creation fails, return an empty string
            if (!isCreateSuccess)
            {
                return string.Empty;
            }

            // Report ID Acquisition
            string reportId = await GetReportId(maxNumberofTries);
            // If no report ID is obtained, return an empty string
            if (reportId == string.Empty)
            {
                return string.Empty;
            }

            // Report Status Acquisition
            bool isComplete = await GetCompleteStatus(maxNumberofTries, reportId);
            // If the report is not complete, return an empty string
            if (!isComplete)
            {
                return string.Empty;
            }

            // Report Download: Download the report and wait for completion
            string reportPath = await blackduckApiService.SaveReport(reportId);
            // Return the report path
            return reportPath;
        }

        public async Task<string> GetReportId(int maxTries)
        {
            var reportId = string.Empty;
            int idTryCount = 0;


            while (reportId == string.Empty && idTryCount < maxTries)
            {
                // Wait for 1 second before each retry
                await Task.Delay(1000);
                reportId = await blackduckApiService.GetLatestVulnerabilityReportId();
                idTryCount++;
            }

            return reportId;
        }

        public async Task<bool> GetCompleteStatus(int maxTries, string reportId)
        {
            bool isComplete = false;
            int statusTryCount = 0;

            // Try to get the latest vulnerability report ID until successful or max tries reached
            while (!isComplete && statusTryCount < maxTries)
            {
                // Wait for 20 seconds before each retry
                await Task.Delay(20000);
                isComplete = await blackduckApiService.GetVulnerabilityStatusReportCompleteStatus(reportId);
                statusTryCount++;
            }

            return isComplete;
        }
    }
}