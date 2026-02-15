using System.Windows;
using Application = System.Windows.Application;

namespace WallpaperApp.TrayApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure only one instance is running
            var mutexName = "WeatherWallpaperTrayApp_SingleInstance";
            var mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "Weather Wallpaper is already running! Check your system tray.",
                    "Already Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }
        }
    }
}
