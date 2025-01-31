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

        public Task<string> DownloadReport()
        {
            var isCreateSuccess = blackduckApiService.CreateVulnerabilityStatusReport();

            if (!isCreateSuccess.Result)
            {
                return Task.FromResult(string.Empty);
            }

            var reportId = string.Empty;
            int tryCount = 0;

            var maxTries = configuration.GetSection(KEY_MAX_TRIES).Get<int>();
            while (reportId == string.Empty && tryCount < maxTries)
            {
                Thread.Sleep(20000);
                reportId = blackduckApiService.GetLatestVulnerabilityReportId().Result;
                tryCount++;
            }

            if (reportId == string.Empty)
            {
                return Task.FromResult(string.Empty);
            }

            Task.Run(() => blackduckApiService.DownloadReport(reportId)).Wait();

            return Task.FromResult(reportId);
        }
    }
}