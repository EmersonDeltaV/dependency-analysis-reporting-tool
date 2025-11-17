using Microsoft.Extensions.Logging;

namespace DART.BlackduckAnalysis
{
    public class BlackduckReportGenerator: IBlackduckReportGenerator
    {
        private readonly IBlackduckReportService blackduckReportService;
        private readonly IFileService fileService;
        private readonly ILogger logger;

        private BlackduckConfiguration? RuntimeConfig;
        private string? ReportFolderPath;
        private string? DestinationPath;

        public BlackduckReportGenerator(IBlackduckReportService blackduckReportService, IFileService fileService, ILogger<BlackduckReportGenerator> logger)
        {
            this.blackduckReportService = blackduckReportService;
            this.fileService = fileService;
            this.logger = logger;
        }

        public void SetRuntimeConfig(BlackduckConfiguration config, string reportFolderPath)
        {
            RuntimeConfig = config;
            ReportFolderPath = reportFolderPath;
        }

        // This method generates a report by downloading a vulnerability report, extracting it, and transferring the extracted files.
        // It also handles cleanup of the downloaded and extracted files, and logs the process.
        public async Task GenerateReport()
        {
            try
            {
                if (RuntimeConfig is null || string.IsNullOrEmpty(ReportFolderPath))
                {
                    throw new InvalidOperationException("Runtime configuration not set. Call SetRuntimeConfig before GenerateReport.");
                }
                // Compute downloads directory under the configured output path
                var downloadsDir = Path.Combine(ReportFolderPath!, BlackduckConfiguration.DownloadsFolderName);

                // Download the vulnerability report and get the report path
                var reportPath = await blackduckReportService.DownloadVulnerabilityReport(RuntimeConfig, downloadsDir);

                // If the report path is empty, throw an exception.
                if (string.IsNullOrEmpty(reportPath)) 
                {
                    throw new Exception("Report download failed.");
                }

                // Extract the downloaded report files.
                var extractResult = fileService.ExtractFiles(reportPath);

                // Transfer the extracted files to the destination path.
                DestinationPath = downloadsDir;
                var transferResult = fileService.TransferFiles(extractResult.DestinationPath, DestinationPath);

                // If the transfer fails, throw an exception.
                if (!transferResult)
                {
                    throw new Exception("Report downloaded but failed to extract.");
                }

                // Log the successful download and extraction.
                logger.LogInformation("Report downloaded and extracted successfully.");

                // Cleanup the downloaded and extracted files.
                fileService.DeleteFile(reportPath);
                fileService.DeleteDirectory(extractResult.DestinationPath);

            }
            catch (Exception ex)
            {
                // Log any errors that occur during the report generation process.
                logger.LogError(ex, "An error occurred while generating the report.");
            }
        }

        public Task Cleanup()
        {
            if (!string.IsNullOrEmpty(DestinationPath) && Directory.Exists(DestinationPath))
            {
                Directory.Delete(DestinationPath, recursive: true);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

    }
}
