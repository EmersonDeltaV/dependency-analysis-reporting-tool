using DART.BlackduckAnalysis.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DART.BlackduckAnalysis
{
    public class BlackduckReportGenerator: IBlackduckReportGenerator
    {
        private const string KEY_REPORT_FOLDER_PATH = "ReportFolderPath";
        private const string DL_FOLDER_PATH = "Downloaded";

        private readonly IBlackduckReportService blackduckReportService;
        private readonly IFileService fileService;
        private readonly ILogger logger;

        private readonly string ReportFolderPath;
        private readonly string DestinationPath;

        public BlackduckReportGenerator(IBlackduckReportService blackduckReportService, IFileService fileService, IConfiguration configuration, ILogger<BlackduckReportGenerator> logger)
        {
            this.blackduckReportService = blackduckReportService;
            this.fileService = fileService;
            this.logger = logger;

            ReportFolderPath = configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value;
            DestinationPath = Path.Combine(ReportFolderPath, DL_FOLDER_PATH);
        }

        // This method generates a report by downloading a vulnerability report, extracting it, and transferring the extracted files.
        // It also handles cleanup of the downloaded and extracted files, and logs the process.
        public async Task GenerateReport()
        {
            try
            {
                // Download the vulnerability report and get the report path
                var reportPath = await blackduckReportService.DownloadVulnerabilityReport();

                // If the report path is empty, throw an exception.
                if (string.IsNullOrEmpty(reportPath)) 
                {
                    throw new Exception("Report download failed.");
                }

                // Extract the downloaded report files.
                var extractResult = fileService.ExtractFiles(reportPath);

                // Transfer the extracted files to the destination path.
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
            Directory.GetFiles(DestinationPath, "*.csv", SearchOption.AllDirectories)
                .ToList().ForEach(File.Delete);
            Directory.Delete(DestinationPath);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

    }
}
