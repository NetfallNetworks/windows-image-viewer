using WallpaperApp.Configuration;

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
                var configService = new ConfigurationService();
                var settings = configService.LoadConfiguration();

                Console.WriteLine("✓ Configuration loaded successfully");
                Console.WriteLine($"  Image URL: {settings.ImageUrl}");
                Console.WriteLine($"  Refresh Interval: {settings.RefreshIntervalMinutes} minutes");
                Console.WriteLine();
                Console.WriteLine("NOTE: Story 2 only reads configuration.");
                Console.WriteLine("      Image download and wallpaper setting come in Story 3 & 4.");
                Console.WriteLine();
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
