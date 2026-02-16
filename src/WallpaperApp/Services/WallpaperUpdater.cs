using WallpaperApp.Configuration;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Orchestrates the complete workflow of fetching and setting wallpaper.
    /// </summary>
    public class WallpaperUpdater : IWallpaperUpdater
    {
        private readonly IConfigurationService _configurationService;
        private readonly IImageFetcher _imageFetcher;
        private readonly IWallpaperService _wallpaperService;
        private readonly IAppStateService _appStateService;
        private readonly IFileCleanupService _fileCleanupService;

        public WallpaperUpdater(
            IConfigurationService configurationService,
            IImageFetcher imageFetcher,
            IWallpaperService wallpaperService,
            IAppStateService appStateService,
            IFileCleanupService fileCleanupService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _imageFetcher = imageFetcher ?? throw new ArgumentNullException(nameof(imageFetcher));
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));
            _fileCleanupService = fileCleanupService ?? throw new ArgumentNullException(nameof(fileCleanupService));
        }

        /// <summary>
        /// Updates the wallpaper by downloading an image from the configured URL and setting it as the desktop wallpaper.
        /// </summary>
        /// <returns>True if the wallpaper was updated successfully, false otherwise.</returns>
        public async Task<bool> UpdateWallpaperAsync()
        {
            try
            {
                // Step 1: Load configuration
                Console.WriteLine("Loading configuration...");
                var settings = _configurationService.LoadConfiguration();
                Console.WriteLine($"✓ Configuration loaded");
                Console.WriteLine($"  Image URL: {settings.ImageUrl}");
                Console.WriteLine();

                // Step 2: Download image
                Console.WriteLine("Downloading image...");
                var downloadedPath = await _imageFetcher.DownloadImageAsync(settings.ImageUrl);

                if (downloadedPath == null)
                {
                    Console.WriteLine("❌ Failed to download image");

                    // Story WS-5: Try last-known-good fallback
                    var state = _appStateService.LoadState();
                    if (!string.IsNullOrEmpty(state.LastKnownGoodImagePath) &&
                        File.Exists(state.LastKnownGoodImagePath))
                    {
                        Console.WriteLine("  Falling back to last-known-good wallpaper...");
                        try
                        {
                            _wallpaperService.SetWallpaper(state.LastKnownGoodImagePath, settings.FitMode);
                            Console.WriteLine($"✓ Using last-known-good: {Path.GetFileName(state.LastKnownGoodImagePath)}");
                            _appStateService.IncrementFailureCount();
                            return true; // Success via fallback
                        }
                        catch (Exception fallbackEx)
                        {
                            Console.WriteLine($"  Fallback also failed: {fallbackEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  No last-known-good wallpaper available.");
                    }

                    Console.WriteLine("  The wallpaper will remain unchanged.");
                    _appStateService.IncrementFailureCount();
                    return false;
                }

                Console.WriteLine("✓ Downloaded image");
                Console.WriteLine($"  Saved to: {downloadedPath}");
                Console.WriteLine();

                // Step 3: Set wallpaper
                Console.WriteLine("Setting wallpaper...");
                _wallpaperService.SetWallpaper(downloadedPath, settings.FitMode); // Story WS-4
                Console.WriteLine("✓ Wallpaper updated");
                Console.WriteLine();

                // Story WS-5: Save as last-known-good on success
                _appStateService.UpdateLastKnownGood(downloadedPath);
                _appStateService.IncrementSuccessCount();

                // Story WS-6: Cleanup old files after successful update
                _fileCleanupService.CleanupOldFiles();

                return true;
            }
            catch (ConfigurationException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Configuration Error:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                return false;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ File Not Found:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                return false;
            }
            catch (InvalidImageException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Invalid Image:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                _appStateService.IncrementFailureCount();
                return false;
            }
            catch (WallpaperException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Wallpaper Error:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                _appStateService.IncrementFailureCount();
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Unexpected Error:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                return false;
            }
        }
    }
}
