using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // Help flag
            if (args.Length > 0 && args[0] == "--help")
            {
                Console.WriteLine("USAGE:");
                Console.WriteLine("  WallpaperApp.exe");
                Console.WriteLine("    Run as Windows Service or console app with periodic wallpaper updates");
                Console.WriteLine();
                Console.WriteLine("  WallpaperApp.exe --download");
                Console.WriteLine("    Downloads image from URL in configuration");
                Console.WriteLine();
                Console.WriteLine("  WallpaperApp.exe <path-to-image>");
                Console.WriteLine("    Sets wallpaper to local image file");
                Console.WriteLine();
                Console.WriteLine("  WallpaperApp.exe --help");
                Console.WriteLine("    Displays this help message");
                Console.WriteLine();
                return 0;
            }
            // Story 7: Windows Service mode (default mode)
            else if (args.Length == 0)
            {
                try
                {
                    var host = CreateHostBuilder(args).Build();
                    await host.RunAsync();
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Fatal error: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    return 1;
                }
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

        /// <summary>
        /// Creates and configures the host builder for the Windows Service.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "WeatherWallpaperService";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register services for dependency injection
                    services.AddSingleton<IConfigurationService, ConfigurationService>();
                    services.AddSingleton<IWallpaperService, WallpaperService>();
                    services.AddHttpClient<IImageFetcher, ImageFetcher>()
                        .ConfigureHttpClient(client =>
                        {
                            client.Timeout = TimeSpan.FromSeconds(30);
                        });
                    services.AddSingleton<IWallpaperUpdater, WallpaperUpdater>();

                    // Register the background worker
                    services.AddHostedService<Worker>();
                });
    }
}
