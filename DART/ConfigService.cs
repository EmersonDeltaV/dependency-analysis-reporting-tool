using BlackduckReportAnalysis.Models;
using Newtonsoft.Json;

namespace BlackduckReportAnalysis
{
    /// <summary>
    /// Provides methods to read and manage the configuration settings.
    /// </summary>
    public static class ConfigService
    {
        /// <summary>
        /// Gets or sets the configuration settings.
        /// </summary>
        public static Config Config { get; private set; }

        /// <summary>
        /// Reads the configuration settings from the config.json file.
        /// </summary>
        /// <exception cref="ConfigException">Thrown when the configuration is invalid or the config.json file is not found.</exception>
        public static void ReadConfigJSON()
        {
            try
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"))!;

                if (string.IsNullOrWhiteSpace(Config.LogPath))
                {
                    throw new ConfigException("Please ensure that the LogPath is provided in config.json.");
                }

                if (string.IsNullOrWhiteSpace(Config.ReportFolderPath) ||
                    string.IsNullOrWhiteSpace(Config.OutputFilePath) ||
                    string.IsNullOrWhiteSpace(Config.BlackduckToken) ||
                    string.IsNullOrWhiteSpace(Config.BaseUrl))
                {
                    throw new ConfigException("Please ensure that all configurations are provided in config.json.");
                }
            }
            catch (FileNotFoundException)
            {
                throw new ConfigException("config.json not found.");
            }
            catch (Exception ex)
            {
                throw new ConfigException($"Encountered exception while reading config.json. {ex}");
            }
        }
    }
}
