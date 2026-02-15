using WallpaperApp.Configuration;
using WallpaperApp.Services;

namespace WallpaperApp
{
    public class Program
    {
        public static int Main(string[] args)
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

                // Story 3: Demonstrate wallpaper service (if test image provided)
                if (args.Length > 0)
                {
                    string testImagePath = args[0];
                    Console.WriteLine($"Demo: Setting wallpaper to: {testImagePath}");

                    var wallpaperService = new WallpaperService();
                    wallpaperService.SetWallpaper(testImagePath);

                    Console.WriteLine("✓ Wallpaper set successfully");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("NOTE: Story 3 implements wallpaper setting.");
                    Console.WriteLine("      Pass an image path as argument to test:");
                    Console.WriteLine("      WallpaperApp.exe <path-to-image.png>");
                    Console.WriteLine();
                    Console.WriteLine("      Image download from URL comes in Story 4.");
                    Console.WriteLine();
                }

                Console.WriteLine("Application completed successfully.");
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

            return 0;
        }
    }
}
