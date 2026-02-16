namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for cleaning up old wallpaper files from the temp directory.
    /// </summary>
    public interface IFileCleanupService
    {
        /// <summary>
        /// Cleans up old wallpaper files based on retention policies.
        /// Keeps the most recent files and deletes files older than the configured age.
        /// </summary>
        /// <param name="maxFiles">Maximum number of files to keep (default: 10).</param>
        /// <param name="maxAgeDays">Maximum age in days to keep files (default: 7).</param>
        void CleanupOldFiles(int maxFiles = 10, int maxAgeDays = 7);
    }
}
