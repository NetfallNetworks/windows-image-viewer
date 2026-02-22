namespace WallpaperApp.Services
{
    /// <summary>
    /// Simple file logger for debugging service issues.
    /// Logs to %LOCALAPPDATA%\WallpaperSync\logs\service.log
    /// </summary>
    public static class FileLogger
    {
        private static readonly string LogDirectory = AppPaths.LogDirectory;
        private static readonly string LogFilePath = AppPaths.LogFile;
        private static readonly object _lock = new object();

        static FileLogger()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
            }
            catch
            {
                // If we can't create the directory, logging will fail silently
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}";

                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);

                    // Also write to console for debugging
                    Console.WriteLine(logMessage);
                }
            }
            catch
            {
                // Logging failures should not crash the service
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var errorMessage = ex != null
                ? $"ERROR: {message} - {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}"
                : $"ERROR: {message}";

            Log(errorMessage);
        }

        public static string GetLogPath()
        {
            return LogFilePath;
        }
    }
}
