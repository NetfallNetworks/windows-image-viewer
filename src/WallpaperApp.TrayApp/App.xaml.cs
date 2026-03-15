using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace WallpaperApp.TrayApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure only one instance is running
            var mutexName = "WallpaperTrayApp_SingleInstance";
            var mutex = new System.Threading.Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // Silently exit — avoids a dialog popup on every reboot
                // if a duplicate startup shortcut launches a second instance.
                Shutdown();
                return;
            }
        }
    }
}
