using BlackduckReportAnalysis;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            ConfigService.ReadConfigJSON();

            SeriLogger.ConfigureSerilog();

            ExcelService.Initialize();

            await CsvService.AnalyzeReport();

            ExcelService.SaveReport();
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
