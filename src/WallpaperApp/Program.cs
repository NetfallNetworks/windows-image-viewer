using WallpaperApp.Configuration;

namespace WallpaperApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Weather Wallpaper App - Starting...");

            try
            {
                var configService = new ConfigurationService();
                var settings = configService.LoadConfiguration();
                Console.WriteLine($"Configured URL: {settings.ImageUrl}");
            }
            catch (ConfigurationException ex)
            {
                Console.Error.WriteLine($"Configuration Error: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
    }
}
