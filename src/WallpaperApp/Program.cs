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

            try
            {
                // Story 2: Load configuration
                var configService = new ConfigurationService();
                var settings = configService.LoadConfiguration();

                Console.WriteLine("✓ Configuration loaded successfully");
                Console.WriteLine($"  Image URL: {settings.ImageUrl}");
                Console.WriteLine($"  Refresh Interval: {settings.RefreshIntervalMinutes} minutes");
                Console.WriteLine();

                // Story 4: Download image from URL (if --download flag provided)
                if (args.Length > 0 && args[0] == "--download")
                {
                    Console.WriteLine($"Story 4: Downloading image from: {settings.ImageUrl}");
                    Console.WriteLine();

                    using var httpClient = new HttpClient();
                    var imageFetcher = new ImageFetcher(httpClient);

                    var downloadedPath = await imageFetcher.DownloadImageAsync(settings.ImageUrl);

                    if (downloadedPath != null)
                    {
                        Console.WriteLine("✓ Image downloaded successfully");
                        Console.WriteLine($"  Saved to: {downloadedPath}");
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to download image");
                        Console.WriteLine("  Check the URL in configuration and try again.");
                        Console.WriteLine();
                        return 1;
                    }
                }
                // Story 3: Demonstrate wallpaper service (if test image provided)
                else if (args.Length > 0)
                {
                    string testImagePath = args[0];
                    Console.WriteLine($"Story 3: Setting wallpaper to: {testImagePath}");

                    var wallpaperService = new WallpaperService();
                    wallpaperService.SetWallpaper(testImagePath);

                    Console.WriteLine("✓ Wallpaper set successfully");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("USAGE:");
                    Console.WriteLine("  WallpaperApp.exe --download");
                    Console.WriteLine("    Downloads image from URL in configuration (Story 4)");
                    Console.WriteLine();
                    Console.WriteLine("  WallpaperApp.exe <path-to-image.png>");
                    Console.WriteLine("    Sets wallpaper to local image file (Story 3)");
                    Console.WriteLine();
                }

                Console.WriteLine("Application completed successfully.");
                return 0;
            }
            catch (ConfigurationException ex)
            {
                Console.WriteLine();
                Console.Error.WriteLine("❌ Configuration Error:");
                Console.Error.WriteLine($"   {ex.Message}");
                Console.WriteLine();
                return 1;
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
    }
}
