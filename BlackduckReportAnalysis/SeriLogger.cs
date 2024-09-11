using Serilog;
using Serilog.Core;

namespace BlackduckReportAnalysis
{
    /// <summary>
    /// Provides logging functionality using Serilog.
    /// </summary>
    public static class SeriLogger
    {
        private static Logger Logger;

        /// <summary>
        /// Configures Serilog with the specified logging options.
        /// </summary>
        public static void ConfigureSerilog()
        {
            Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(ConfigService.Config.LogPath, $"log-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt"))
                .CreateLogger();
        }

        /// <summary>
        /// Writes an information-level log message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public static void Information(string message)
        {
            Logger.Information(message);
        }

        /// <summary>
        /// Writes an error-level log message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public static void Error(string message)
        {
            Logger.Error(message);
        }

        /// <summary>
        /// Writes a warning-level log message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public static void Warning(string message)
        {
            Logger.Warning(message);
        }
    }
}
