using DART.BlackduckAnalysis;
using DART.Console;
using DART.Core;
using DART.EOLAnalysis;
using DART.ReportGenerator.DependencyInjection;
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

        var configuration = ConfigurationFactory.BuildConfiguration(Directory.GetCurrentDirectory());

        builder.Configuration.AddConfiguration(configuration);

        // Create log directory and configure Serilog with user's LogPath
        var logPath = configuration.GetValue<string>("ReportConfiguration:LogPath");
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

        builder.Services.AddSingleton<IAnalysisOrchestrator, AnalysisOrchestrator>();
        builder.Services.AddBlackduckAnalysis();
        builder.Services.AddEolAnalysis();
        builder.Services.AddReportGenerator();

        builder.Services.Configure<Config>(configuration);
        // Register BlackduckConfiguration separately from the nested Config property
        builder.Services.Configure<BlackduckConfiguration>(configuration.GetSection("BlackduckConfiguration"));
        builder.Services.Configure<ReportConfiguration>(configuration.GetSection("ReportConfiguration"));
        builder.Services.Configure<EOLAnalysisConfig>(configuration.GetSection("EOLAnalysis"));
        builder.Services.Configure<FeatureToggles>(configuration.GetSection("FeatureToggles"));
        builder.Services.AddHostedService<DartOrchestrator>();
        builder.Services.AddSingleton<IConfiguration>(configuration);

        using IHost host = builder.Build();

        await host.RunAsync();
    }
}
