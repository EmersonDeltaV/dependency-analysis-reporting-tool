using BlackduckReportGeneratorTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackduckReportGeneratorTool
{
    public class Startup(IBlackduckReportService blackduckReportService,
        IFileService fileService, IConfiguration configuration,
        ILogger<Startup> logger) : IHostedService
    {
        private const string KEY_REPORT_FOLDER_PATH = "ReportFolderPath";

        private readonly IBlackduckReportService blackduckReportService = blackduckReportService;
        private readonly IFileService fileService = fileService;
        private readonly IConfiguration configuration = configuration;
        private readonly ILogger logger = logger;

        public Task StartAsync(CancellationToken cancellationToken)
        {

            var reportId = blackduckReportService.DownloadReport().Result;

            var filePath = Path.Combine(Environment.CurrentDirectory, $"downloaded\\{reportId}.zip");

            var extractResult = fileService.ExtractFiles(filePath);

            var transferResult = fileService.TransferFiles(extractResult.ResultPath,
                configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value);

            if (transferResult)
            {
                logger.LogInformation("Report downloaded and extracted successfully.");
            }
            else
            {
                logger.LogError("Report downloaded but failed to extract.");
            }

            // Cleanup
            fileService.DeleteFile(filePath);
            fileService.DeleteDirectory(extractResult.ResultPath);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

    }
}
