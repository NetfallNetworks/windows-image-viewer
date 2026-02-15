namespace WallpaperApp.Services
{
    /// <summary>
    /// Orchestrates the complete workflow of fetching and setting wallpaper.
    /// </summary>
    public interface IWallpaperUpdater
    {
        /// <summary>
        /// Updates the wallpaper by downloading an image from the configured URL and setting it as the desktop wallpaper.
        /// </summary>
        /// <returns>True if the wallpaper was updated successfully, false otherwise.</returns>
        Task<bool> UpdateWallpaperAsync();
    }
}
