using BlackduckReportAnalysis;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            ConfigService.ReadConfigJSON();

            SeriLogger.ConfigureSerilog();

            if(ConfigService.Config.PreviousResults == string.Empty || ConfigService.Config.CurrentResults == string.Empty)
            {
                
                SeriLogger.Information("No previous results found. Skipping comparison.");

                ExcelService.Initialize();
                await CsvService.AnalyzeReport();
                ExcelService.SaveReport();

                return;
            }
            else
            {
                ExcelService.CompareExcelFiles(ConfigService.Config.CurrentResults, ConfigService.Config.PreviousResults, ConfigService.Config.OutputFilePath);
            }
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
