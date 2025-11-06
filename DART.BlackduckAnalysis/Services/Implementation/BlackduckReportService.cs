namespace DART.BlackduckAnalysis
{
    public class BlackduckReportService : IBlackduckReportService
    {
        private readonly IBlackduckApiService blackduckApiService;

        public BlackduckReportService(IBlackduckApiService blackduckApiService)
        {
            this.blackduckApiService = blackduckApiService;
        }

        public async Task<string> DownloadVulnerabilityReport(BlackduckConfiguration config, string reportFolderPath)
        {
            var isCreateSuccess = await blackduckApiService.CreateVulnerabilityStatusReport(config);
            if (!isCreateSuccess)
            {
                return string.Empty;
            }

            int maxTries = config.DownloadParameters?.MaxTries ?? 20;
            int pollingDelayMs = config.DownloadParameters?.PollingDelayMilliseconds ?? 5000;

            string reportId = await GetReportId(config, maxTries, pollingDelayMs);
            if (reportId == string.Empty)
            {
                return string.Empty;
            }

            bool isComplete = await GetCompleteStatus(config, maxTries, pollingDelayMs, reportId);
            if (!isComplete)
            {
                return string.Empty;
            }

            string reportPath = await blackduckApiService.SaveReport(config, reportId, reportFolderPath);
            return reportPath;
        }

        public async Task<string> GetReportId(BlackduckConfiguration config, int maxTries, int pollingDelayMs)
        {
            var reportId = string.Empty;
            int idTryCount = 0;

            while (reportId == string.Empty && idTryCount < maxTries)
            {
                await Task.Delay(pollingDelayMs);
                reportId = await blackduckApiService.GetLatestVulnerabilityReportId(config);
                idTryCount++;
            }

            return reportId;
        }

        public async Task<bool> GetCompleteStatus(BlackduckConfiguration config, int maxTries, int pollingDelayMs, string reportId)
        {
            bool isComplete = false;
            int statusTryCount = 0;

            while (!isComplete && statusTryCount < maxTries)
            {
                await Task.Delay(pollingDelayMs);
                isComplete = await blackduckApiService.GetVulnerabilityStatusReportCompleteStatus(config, reportId);
                statusTryCount++;
            }

            return isComplete;
        }
    }
}
