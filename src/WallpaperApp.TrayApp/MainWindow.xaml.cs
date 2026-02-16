using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingIcon = System.Drawing.Icon;

namespace WallpaperApp.TrayApp
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private System.Threading.Timer? _updateTimer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWallpaperUpdater _wallpaperUpdater;
        private readonly IConfigurationService _configurationService;
        private DateTime _nextRefreshTime;
        private string _status = "Initializing...";

        public MainWindow()
        {
            InitializeComponent();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Get services
            _wallpaperUpdater = _serviceProvider.GetRequiredService<IWallpaperUpdater>();
            _configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();

            // Initialize system tray
            InitializeTrayIcon();

            // Start wallpaper updates
            StartWallpaperUpdates();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<IImageValidator, ImageValidator>();
            services.AddSingleton<IAppStateService, AppStateService>();
            services.AddSingleton<IFileCleanupService, FileCleanupService>();
            services.AddSingleton<IWallpaperService, WallpaperService>();
            services.AddHttpClient<IImageFetcher, ImageFetcher>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            services.AddSingleton<IWallpaperUpdater, WallpaperUpdater>();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateDefaultIcon(),
                Visible = true,
                Text = "Wallpaper"
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            var refreshItem = new ToolStripMenuItem("🔄 Refresh Now");
            refreshItem.Click += async (s, e) => await RefreshNowAsync();
            contextMenu.Items.Add(refreshItem);

            var statusItem = new ToolStripMenuItem("📊 Status");
            statusItem.Click += (s, e) => ShowStatus();
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var openFolderItem = new ToolStripMenuItem("📁 Open Image Folder");
            openFolderItem.Click += (s, e) => OpenImageFolder();
            contextMenu.Items.Add(openFolderItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("ℹ️ About");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("❌ Exit");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Double-click to show status
            _notifyIcon.DoubleClick += (s, e) => ShowStatus();
        }

        private DrawingIcon CreateDefaultIcon()
        {
            // Create a simple icon (you can replace this with a real .ico file)
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(0, 120, 212)); // Windows blue
                g.DrawString("W", new DrawingFont("Segoe UI", 20, DrawingFontStyle.Bold), Brushes.White, new PointF(4, 2));
            }
            return DrawingIcon.FromHandle(bitmap.GetHicon());
        }

        private async void StartWallpaperUpdates()
        {
            try
            {
                FileLogger.Log("=== Tray App Starting ===");

                // Load configuration
                var settings = _configurationService.LoadConfiguration();
                var intervalMilliseconds = settings.RefreshIntervalMinutes * 60 * 1000;

                FileLogger.Log($"Refresh interval: {settings.RefreshIntervalMinutes} minutes");

                // Execute first update
                FileLogger.Log("Executing first wallpaper update...");
                _status = "Updating wallpaper...";
                UpdateTrayIconText();

                await ExecuteUpdateAsync();

                _status = "Running";
                UpdateTrayIconText();

                // Calculate next refresh
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                FileLogger.Log($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");

                // Start timer for subsequent updates
                _updateTimer = new System.Threading.Timer(
                    callback: async _ => await TimerCallbackAsync(),
                    state: null,
                    dueTime: intervalMilliseconds,
                    period: intervalMilliseconds
                );
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Failed to start wallpaper updates", ex);
                _status = "Error";
                UpdateTrayIconText();
                MessageBox.Show(
                    $"Failed to start wallpaper updates:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task TimerCallbackAsync()
        {
            try
            {
                FileLogger.Log("Timer triggered - updating wallpaper...");
                await ExecuteUpdateAsync();

                var settings = _configurationService.LoadConfiguration();
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                FileLogger.Log($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Error in timer callback", ex);
            }
        }

        private async Task ExecuteUpdateAsync()
        {
            try
            {
                await _wallpaperUpdater.UpdateWallpaperAsync();
                FileLogger.Log("Wallpaper update completed successfully");
                _status = "Running";
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Wallpaper update failed", ex);
                _status = "Update failed";
            }

            UpdateTrayIconText();
        }

        private async Task RefreshNowAsync()
        {
            try
            {
                _status = "Refreshing...";
                UpdateTrayIconText();

                FileLogger.Log("Manual refresh requested");
                await ExecuteUpdateAsync();

                ShowBalloonTip("Wallpaper Updated", "Your wallpaper has been refreshed!", ToolTipIcon.Info);

                // Reset the timer
                var settings = _configurationService.LoadConfiguration();
                _nextRefreshTime = DateTime.Now.AddMinutes(settings.RefreshIntervalMinutes);
                FileLogger.Log($"Next refresh at: {_nextRefreshTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Manual refresh failed", ex);
                ShowBalloonTip("Update Failed", ex.Message, ToolTipIcon.Error);
            }
        }

        private void ShowStatus()
        {
            var settings = _configurationService.LoadConfiguration();
            var timeUntilNext = _nextRefreshTime - DateTime.Now;

            var statusMessage = $"Wallpaper Status\n\n" +
                              $"Status: {_status}\n" +
                              $"Refresh Interval: {settings.RefreshIntervalMinutes} minutes\n" +
                              $"Next Refresh: {_nextRefreshTime:HH:mm:ss}\n" +
                              $"Time Until Next: {timeUntilNext.Hours}h {timeUntilNext.Minutes}m {timeUntilNext.Seconds}s\n\n" +
                              $"Image URL: {settings.ImageUrl}\n" +
                              $"Log File: {FileLogger.GetLogPath()}";

            MessageBox.Show(statusMessage, "Wallpaper Status", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Wallpaper Tray App\n\n" +
                "Automatically updates your wallpaper!\n\n" +
                "Right-click the tray icon for options.\n" +
                "Double-click to view status.",
                "About Wallpaper",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OpenImageFolder()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "WallpaperService");
                if (Directory.Exists(tempPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", tempPath);
                }
                else
                {
                    MessageBox.Show(
                        "Image folder not found yet. Try refreshing the wallpaper first!",
                        "Folder Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTrayIconText()
        {
            if (_notifyIcon != null)
            {
                var tooltip = $"Wallpaper - {_status}";
                // Tooltip max length is 63 characters
                if (tooltip.Length > 63)
                    tooltip = tooltip.Substring(0, 60) + "...";

                _notifyIcon.Text = tooltip;
            }
        }

        private void ShowBalloonTip(string title, string message, ToolTipIcon icon)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        private void ExitApplication()
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit?\n\nWallpaper updates will stop until you restart the app.",
                "Exit Wallpaper",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                FileLogger.Log("User requested exit");
                _updateTimer?.Dispose();
                _notifyIcon?.Dispose();
                Application.Current.Shutdown();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateTimer?.Dispose();
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
