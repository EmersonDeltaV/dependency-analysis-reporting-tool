using BlackduckReportGeneratorTool.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlackduckReportGeneratorTool
{
    public class BlackduckReportGenerator(IBlackduckReportService blackduckReportService,
        IFileService fileService, IConfiguration configuration,
        ILogger<BlackduckReportGenerator> logger) : IBlackduckReportGenerator
    {
        private const string KEY_REPORT_FOLDER_PATH = "ReportFolderPath";
        private const string DL_FOLDER_PATH = "Downloaded";

        private readonly IBlackduckReportService blackduckReportService = blackduckReportService;
        private readonly IFileService fileService = fileService;
        private readonly IConfiguration configuration = configuration;
        private readonly ILogger logger = logger;

        public void GenerateReport()
        {

            var reportId = blackduckReportService.DownloadReport().Result;

            var filePath = Path.Combine(Environment.CurrentDirectory, configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value, DL_FOLDER_PATH, $"{reportId}.zip");

            var extractResult = fileService.ExtractFiles(filePath);

            var transferResult = fileService.TransferFiles(extractResult.ResultPath,
                Path.Combine(configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value, DL_FOLDER_PATH));

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

            return; 
        }

        public void Cleanup()
        {
            Directory.GetFiles(configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value, "*.csv", SearchOption.AllDirectories).ToList().ForEach(File.Delete);
            Directory.Delete(Path.Combine(configuration.GetSection(KEY_REPORT_FOLDER_PATH).Value, DL_FOLDER_PATH));
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

    }
}
