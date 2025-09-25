using Serilog;
using Serilog.Core;

namespace BlackduckReportAnalysis
{
    /// <summary>
    /// Provides logging functionality using Serilog.
    /// </summary>
    public static class SeriLogger
    {
        private static Logger? _logger;
        private static readonly object _lockObject = new object();

        private static Logger Logger
        {
            get
            {
                if (_logger == null)
                {
                    lock (_lockObject)
                    {
                        if (_logger == null)
                        {
                            // Initialize config if not already done
                            if (ConfigService.Config == null)
                            {
                                ConfigService.ReadConfigJSON();
                            }
                            _logger = new LoggerConfiguration()
                                .WriteTo.Console()
                                .WriteTo.File(Path.Combine(ConfigService.Config.LogPath, $"log-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt"))
                                .CreateLogger();
                        }
                    }
                }
                return _logger;
            }
        }

        /// <summary>
        /// Configures Serilog with the specified logging options.
        /// </summary>
        [Obsolete("SeriLogger now initializes automatically. This method is no longer needed.")]
        public static void ConfigureSerilog()
        {
            // This method is now obsolete as the logger initializes automatically
            _ = Logger; // Force initialization
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
