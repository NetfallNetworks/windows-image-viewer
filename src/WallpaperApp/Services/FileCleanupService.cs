namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for cleaning up old wallpaper files from the temp directory.
    /// Removes files based on count and age retention policies.
    /// </summary>
    public class FileCleanupService : IFileCleanupService
    {
        private readonly string _tempDirectory;

        /// <summary>
        /// Initializes a new instance of FileCleanupService.
        /// </summary>
        public FileCleanupService()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "Wallpaper");
        }

        /// <summary>
        /// For testing: allows overriding the temp directory path.
        /// </summary>
        public FileCleanupService(string tempDirectory)
        {
            _tempDirectory = tempDirectory;
        }

        /// <summary>
        /// Cleans up old wallpaper files based on retention policies.
        /// Keeps the most recent files and deletes files older than the configured age.
        /// </summary>
        /// <param name="maxFiles">Maximum number of files to keep (default: 10).</param>
        /// <param name="maxAgeDays">Maximum age in days to keep files (default: 7).</param>
        public void CleanupOldFiles(int maxFiles = 10, int maxAgeDays = 7)
        {
            if (!Directory.Exists(_tempDirectory))
            {
                FileLogger.Log("Cleanup: Temp directory doesn't exist, nothing to clean");
                return;
            }

            try
            {
                // Get all wallpaper files sorted by creation time (newest first)
                var files = Directory.GetFiles(_tempDirectory, "wallpaper-*.png")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (files.Count == 0)
                {
                    FileLogger.Log("Cleanup: No wallpaper files found");
                    return;
                }

                FileLogger.Log($"Cleanup: Found {files.Count} wallpaper files");

                int deletedCount = 0;
                var cutoffDate = DateTime.Now.AddDays(-maxAgeDays);

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    bool shouldDelete = false;
                    string reason = "";

                    // Delete if beyond maxFiles limit
                    if (i >= maxFiles)
                    {
                        shouldDelete = true;
                        reason = $"exceeds max file count ({maxFiles})";
                    }
                    // Delete if older than maxAgeDays
                    else if (file.CreationTime < cutoffDate)
                    {
                        shouldDelete = true;
                        reason = $"older than {maxAgeDays} days";
                    }

                    if (shouldDelete)
                    {
                        try
                        {
                            file.Delete();
                            deletedCount++;
                            FileLogger.Log($"Cleanup: Deleted {file.Name} ({reason})");
                        }
                        catch (Exception ex)
                        {
                            FileLogger.Log($"Cleanup: Failed to delete {file.Name}: {ex.Message}");
                        }
                    }
                }

                FileLogger.Log($"Cleanup: Deleted {deletedCount} files, kept {files.Count - deletedCount} files");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Cleanup: Error during cleanup: {ex.Message}");
            }
        }
    }
}
