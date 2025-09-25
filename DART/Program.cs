using BlackduckReportAnalysis;
using BlackduckReportGeneratorTool.Services.Implementation;
using Serilog;
using BlackduckReportGeneratorTool.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlackduckReportGeneratorTool;
using Microsoft.Extensions.Configuration;
using DART.EOLAnalysis;

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

        builder.Services.AddHostedService<BlackduckReportAnalysisProgram>();
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddSingleton<ICsvService, CsvService>();
        builder.Services.AddSingleton<IExcelService, ExcelService>();
        builder.Services.AddSingleton<IBlackduckReportGenerator, BlackduckReportGenerator>();
        builder.Services.AddSingleton<IBlackduckReportService, BlackduckReportService>();
        builder.Services.AddSingleton<IBlackduckApiService, BlackduckReportGeneratorTool.Integration.Implementation.BlackduckApiService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddScoped<IEOLAnalysisService, EOLAnalysisService>();

        using IHost host = builder.Build();

        host.Start();

        return;

    }
}
