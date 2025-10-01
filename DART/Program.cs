using BlackduckReportAnalysis;
using BlackduckReportAnalysis.Models;
using BlackduckReportGeneratorTool;
using BlackduckReportGeneratorTool.Services.Implementation;
using BlackduckReportGeneratorTool.Services.Interfaces;
using DART.EOLAnalysis;
using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Services;
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

        var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
                                                .Enrich.FromLogContext()
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
        builder.Services.AddSingleton<IBlackduckApiService, BlackduckReportGeneratorTool.Integration.Implementation.BlackduckApiService>();
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
