using WallpaperApp.Configuration;
using WallpaperApp.Services;

namespace WallpaperApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task<int> MainAsync(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Weather Wallpaper App");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"Looking for: WallpaperApp.json");
            Console.WriteLine();

            // Story 5: Integrated workflow - fetch and set wallpaper (default mode)
            if (args.Length == 0)
            {
                Console.WriteLine("Story 5: Fetching and setting wallpaper...");
                Console.WriteLine();

                var configService = new ConfigurationService();
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var imageFetcher = new ImageFetcher(httpClient);
                var wallpaperService = new WallpaperService();

                var updater = new WallpaperUpdater(configService, imageFetcher, wallpaperService);
                bool success = await updater.UpdateWallpaperAsync();

                return success ? 0 : 1;
            }
            // Story 4: Download image from URL (if --download flag provided)
            else if (args.Length > 0 && args[0] == "--download")
            {
                try
                {
                    var configService = new ConfigurationService();
                    var settings = configService.LoadConfiguration();

                    Console.WriteLine("✓ Configuration loaded successfully");
                    Console.WriteLine($"  Image URL: {settings.ImageUrl}");
                    Console.WriteLine();

                    Console.WriteLine($"Story 4: Downloading image from: {settings.ImageUrl}");

                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                    var imageFetcher = new ImageFetcher(httpClient);

                    var downloadedPath = await imageFetcher.DownloadImageAsync(settings.ImageUrl);

                    if (downloadedPath != null)
                    {
                        Console.WriteLine("✓ Image downloaded successfully");
                        Console.WriteLine($"  Saved to: {downloadedPath}");
                        Console.WriteLine();
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to download image");
                        Console.WriteLine("  Check the URL in configuration and try again.");
                        Console.WriteLine();
                        return 1;
                    }
                }
                catch (ConfigurationException ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("❌ Configuration Error:");
                    Console.Error.WriteLine($"   {ex.Message}");
                    Console.WriteLine();
                    return 1;
                }
            }
            // Story 3: Demonstrate wallpaper service (if test image provided)
            else if (args.Length > 0)
            {
                try
                {
                    string testImagePath = args[0];
                    Console.WriteLine($"Story 3: Setting wallpaper to: {testImagePath}");
                    Console.WriteLine();

                    var wallpaperService = new WallpaperService();
                    wallpaperService.SetWallpaper(testImagePath);

                    Console.WriteLine("✓ Wallpaper set successfully");
                    Console.WriteLine();
                    return 0;
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("❌ File Not Found:");
                    Console.Error.WriteLine($"   {ex.Message}");
                    Console.WriteLine();
                    return 1;
                }
                catch (InvalidImageException ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("❌ Invalid Image:");
                    Console.Error.WriteLine($"   {ex.Message}");
                    Console.WriteLine();
                    return 1;
                }
                catch (WallpaperException ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("❌ Wallpaper Error:");
                    Console.Error.WriteLine($"   {ex.Message}");
                    Console.WriteLine();
                    return 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.Error.WriteLine("❌ Unexpected Error:");
                    Console.Error.WriteLine($"   {ex.Message}");
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("Stack Trace:");
                    Console.Error.WriteLine(ex.StackTrace);
                    Console.WriteLine();
                    return 1;
                }
            }

            // Should never reach here
            return 1;
        }
    }
}
