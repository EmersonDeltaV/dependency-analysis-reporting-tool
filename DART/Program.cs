using DART;
using DART.BlackduckAnalysis;
using DART.EOLAnalysis;
using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Services;
using DART.Models;
using DART.Services.Implementation;
using DART.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("config.json")
            .Build();

        builder.Configuration.AddConfiguration(configuration);

        // Create log directory and configure Serilog with user's LogPath
        var logPath = configuration.GetValue<string>("LogPath");
        if (!string.IsNullOrEmpty(logPath) && !Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }

        var fullLogPath = Path.Combine(logPath ?? "Log", "dart_.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .WriteTo.File(fullLogPath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message}{NewLine}{Exception}",
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 4194304,
                retainedFileCountLimit: 10,
                rollingInterval: RollingInterval.Day)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}")
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger);

        builder.Services.Configure<Config>(configuration);
        builder.Services.AddHostedService<DartOrchestrator>();
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddSingleton<ICsvService, CsvService>();
        builder.Services.AddSingleton<IExcelService, ExcelService>();
        builder.Services.AddSingleton<IBlackduckReportGenerator, BlackduckReportGenerator>();
        builder.Services.AddSingleton<IBlackduckReportService, BlackduckReportService>();
        builder.Services.AddSingleton<IBlackduckApiService, BlackduckApiService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IEOLAnalysisService, EOLAnalysisService>();
        builder.Services.AddSingleton<INugetMetadataService, NugetMetadataService>();
        builder.Services.AddSingleton<IAzureDevOpsClientFactory, AzureDevOpsClientFactory>();
        builder.Services.AddSingleton<IRepositoryProcessorService, RepositoryProcessorService>();
        builder.Services.AddSingleton<IProjectAnalysisService, ProjectAnalysisService>();
        builder.Services.AddSingleton<IPackageRecommendationService, PackageRecommendationService>();

        using IHost host = builder.Build();

        await host.StartAsync();

        Console.WriteLine("Press any key to close this window...");
        Console.ReadLine();

        await host.StopAsync();

    }
}
