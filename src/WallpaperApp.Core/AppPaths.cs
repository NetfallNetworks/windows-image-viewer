namespace WallpaperApp
{
    /// <summary>
    /// Central registry for all application storage paths.
    ///
    /// All persistent data lives under a single base directory:
    ///   %LOCALAPPDATA%\WallpaperSync\
    ///
    /// Layout:
    ///   WallpaperApp.json        – user configuration (survives reinstalls)
    ///   state.json               – runtime state (enabled flag, counters, last image)
    ///   logs\service.log         – diagnostic log
    ///   wallpapers\              – downloaded wallpaper images
    /// </summary>
    public static class AppPaths
    {
        /// <summary>
        /// Root data directory: %LOCALAPPDATA%\WallpaperSync\
        /// All application data lives here.
        /// </summary>
        public static string BaseDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WallpaperSync");

        /// <summary>
        /// User configuration file: %LOCALAPPDATA%\WallpaperSync\WallpaperApp.json
        /// </summary>
        public static string ConfigFile => Path.Combine(BaseDirectory, "WallpaperApp.json");

        /// <summary>
        /// Runtime state file: %LOCALAPPDATA%\WallpaperSync\state.json
        /// </summary>
        public static string StateFile => Path.Combine(BaseDirectory, "state.json");

        /// <summary>
        /// Log directory: %LOCALAPPDATA%\WallpaperSync\logs\
        /// </summary>
        public static string LogDirectory => Path.Combine(BaseDirectory, "logs");

        /// <summary>
        /// Log file: %LOCALAPPDATA%\WallpaperSync\logs\service.log
        /// </summary>
        public static string LogFile => Path.Combine(LogDirectory, "service.log");

        /// <summary>
        /// Downloaded wallpaper directory: %LOCALAPPDATA%\WallpaperSync\wallpapers\
        /// </summary>
        public static string WallpaperDirectory => Path.Combine(BaseDirectory, "wallpapers");
    }
}
