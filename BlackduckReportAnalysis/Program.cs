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

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Configuration
            ConfigService.ReadConfigJSON();

            SeriLogger.ConfigureSerilog();

            if (ConfigService.Config.PreviousResults == string.Empty || ConfigService.Config.CurrentResults == string.Empty)
            {
                // Download Report   
                if (ConfigService.Config.FeatureToggles.EnableDownloadTool)
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

                    builder.Services.AddHostedService<Startup>();
                    builder.Services.AddSingleton<IBlackduckReportService, BlackduckReportService>();
                    builder.Services.AddSingleton<IBlackduckApiService, BlackduckReportGeneratorTool.Integration.Implementation.BlackduckApiService>();
                    builder.Services.AddSingleton<IFileService, FileService>();

                    using IHost host = builder.Build();

                    host.Start();

                }

                SeriLogger.Information("No previous results found. Skipping comparison.");

                ExcelService.Initialize();
                await CsvService.AnalyzeReport();
                ExcelService.SaveReport();

                if (ConfigService.Config.FeatureToggles.EnableDownloadTool)
                {
                    Directory.GetFiles(ConfigService.Config.ReportFolderPath.ToLower(), "*.csv", SearchOption.AllDirectories).ToList().ForEach(File.Delete);
                }

            }
            else
            {
                ExcelService.CompareExcelFiles(ConfigService.Config.CurrentResults, ConfigService.Config.PreviousResults, ConfigService.Config.OutputFilePath);
            }

            return;
        }
        catch(HttpRequestException)
        {
            SeriLogger.Error($"Could not reach {ConfigService.Config.BaseUrl}. Please ensure that you are connected to the corporate VPN.");
        }
        catch (ConfigException ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        catch (Exception ex)
        {
            SeriLogger.Error($"Encountered an exception: {ex.Message}");
        }

        Console.WriteLine("Press any key to close this window...");
        Console.ReadLine();
    }
}
