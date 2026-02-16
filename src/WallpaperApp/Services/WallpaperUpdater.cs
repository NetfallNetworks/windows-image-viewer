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

        public WallpaperUpdater(
            IConfigurationService configurationService,
            IImageFetcher imageFetcher,
            IWallpaperService wallpaperService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _imageFetcher = imageFetcher ?? throw new ArgumentNullException(nameof(imageFetcher));
            _wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
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
                    Console.WriteLine("  The wallpaper will remain unchanged.");
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
                return false;
            }
            catch (WallpaperException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Wallpaper Error:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
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
