namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for managing Windows startup configuration.
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Checks if the application is configured to run at Windows startup.
        /// </summary>
        /// <returns>True if startup is enabled, false otherwise.</returns>
        bool IsStartupEnabled();

        /// <summary>
        /// Enables the application to run at Windows startup.
        /// Creates a shortcut in the Startup folder.
        /// </summary>
        void EnableStartup();

        /// <summary>
        /// Disables the application from running at Windows startup.
        /// Removes the shortcut from the Startup folder.
        /// </summary>
        void DisableStartup();
    }
}
