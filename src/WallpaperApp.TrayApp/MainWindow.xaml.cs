using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using WallpaperApp.TrayApp.Views;
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
        private System.Windows.Threading.DispatcherTimer? _clickTimer;
        private bool _doubleClickHandled;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWallpaperUpdater _wallpaperUpdater;
        private readonly IConfigurationService _configurationService;
        private readonly IAppStateService _appStateService;
        private readonly IWallpaperService _wallpaperService;
        private readonly IImageValidator _imageValidator;
        private readonly IStartupService _startupService;
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
            _appStateService = _serviceProvider.GetRequiredService<IAppStateService>();
            _wallpaperService = _serviceProvider.GetRequiredService<IWallpaperService>();
            _imageValidator = _serviceProvider.GetRequiredService<IImageValidator>();
            _startupService = _serviceProvider.GetRequiredService<IStartupService>();

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
            services.AddSingleton<IStartupService, StartupService>();
            services.AddHttpClient<IImageFetcher, ImageFetcher>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                });
            services.AddSingleton<IWallpaperUpdater, WallpaperUpdater>();
        }

        private void InitializeTrayIcon()
        {
            // Load current state to set initial icon
            var state = _appStateService.LoadState();

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateTrayIcon(state.IsEnabled),
                Visible = true,
                Text = state.IsEnabled ? "Wallpaper Sync" : "Wallpaper Sync (Disabled)"
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();

            // Enable/Disable Toggle
            var toggleItem = new ToolStripMenuItem("Enabled");
            toggleItem.CheckOnClick = true;
            toggleItem.Checked = state.IsEnabled;
            toggleItem.Click += OnToggleEnabled;
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var refreshItem = new ToolStripMenuItem("🔄 Refresh Now");
            refreshItem.Click += async (s, e) => await RefreshNowAsync();
            contextMenu.Items.Add(refreshItem);

            var statusItem = new ToolStripMenuItem("📊 Status");
            statusItem.Click += (s, e) => ShowStatus();
            contextMenu.Items.Add(statusItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("⚙️ Settings");
            settingsItem.Click += (s, e) => ShowSettingsWindow();
            contextMenu.Items.Add(settingsItem);

            var openFolderItem = new ToolStripMenuItem("📁 Open Image Folder");
            openFolderItem.Click += (s, e) => OpenImageFolder();
            contextMenu.Items.Add(openFolderItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Run at Startup");
            startupItem.CheckOnClick = true;
            startupItem.Click += OnToggleStartup;
            contextMenu.Items.Add(startupItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("ℹ️ About");
            aboutItem.Click += (s, e) => ShowAbout();
            contextMenu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("❌ Exit");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            // Update checkmarks when menu opens
            contextMenu.Opening += (s, e) =>
            {
                var currentState = _appStateService.LoadState();
                toggleItem.Checked = currentState.IsEnabled;
                startupItem.Checked = _startupService.IsStartupEnabled();
            };

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Initialize click timer for distinguishing single vs double-click
            _clickTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(System.Windows.Forms.SystemInformation.DoubleClickTime)
            };
            _clickTimer.Tick += (s, e) =>
            {
                _clickTimer?.Stop();
                if (!_doubleClickHandled)
                {
                    ShowSettingsWindow();
                }
                _doubleClickHandled = false;
            };

            // Left-click to open settings (with delay to detect double-click)
            _notifyIcon.Click += OnTrayIconClick;

            // Double-click to show status
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        private void OnTrayIconClick(object? sender, EventArgs e)
        {
            // Only handle left-click (right-click shows context menu)
            if (sender is NotifyIcon notifyIcon && e is MouseEventArgs mouseEvent)
            {
                if (mouseEvent.Button == MouseButtons.Left)
                {
                    // Start timer to detect if this is part of a double-click
                    _doubleClickHandled = false;
                    _clickTimer?.Stop();
                    _clickTimer?.Start();
                }
            }
        }

        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            // Mark that double-click was handled to prevent single-click action
            _doubleClickHandled = true;
            _clickTimer?.Stop();
            ShowStatus();
        }

        private void ShowSettingsWindow()
        {
            try
            {
                var settingsWindow = new SettingsWindow(
                    _configurationService,
                    _appStateService,
                    _wallpaperService,
                    _imageValidator);

                settingsWindow.Show();
                settingsWindow.Activate(); // Bring to front
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Failed to open settings window", ex);
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DrawingIcon CreateTrayIcon(bool isEnabled)
        {
            try
            {
                // Try to load the icon from WPF resources via pack URI
                var uri = new Uri("pack://application:,,,/Resources/app.ico");
                var streamInfo = Application.GetResourceStream(uri);

                using (var stream = streamInfo?.Stream)
                {
                    if (stream != null)
                    {
                        // Load the icon and optionally apply grayscale if disabled
                        var icon = new DrawingIcon(stream);

                        // If disabled, create a grayed-out version
                        if (!isEnabled)
                        {
                            return CreateGrayscaleIcon(icon);
                        }

                        return icon;
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Failed to load icon from resources, using fallback", ex);
            }

            // Fallback: Create a simple icon programmatically
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                // Choose color based on enabled state
                var bgColor = isEnabled
                    ? Color.FromArgb(0, 120, 212)   // Blue (#0078D4)
                    : Color.FromArgb(128, 128, 128); // Gray (#808080)

                g.Clear(bgColor);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var font = new DrawingFont("Segoe UI", 20, DrawingFontStyle.Bold);
                g.DrawString("W", font, Brushes.White, new PointF(4, 2));
            }
            return DrawingIcon.FromHandle(bitmap.GetHicon());
        }

        private DrawingIcon CreateGrayscaleIcon(DrawingIcon colorIcon)
        {
            // Convert icon to grayscale bitmap for disabled state
            var bitmap = colorIcon.ToBitmap();
            var grayBitmap = new Bitmap(bitmap.Width, bitmap.Height);

            using (var g = Graphics.FromImage(grayBitmap))
            {
                // Create grayscale color matrix
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] {0.3f, 0.3f, 0.3f, 0, 0},
                    new float[] {0.59f, 0.59f, 0.59f, 0, 0},
                    new float[] {0.11f, 0.11f, 0.11f, 0, 0},
                    new float[] {0, 0, 0, 0.5f, 0},  // 50% opacity
                    new float[] {0, 0, 0, 0, 1}
                });

                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(bitmap,
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    0, 0, bitmap.Width, bitmap.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }

            bitmap.Dispose();
            return DrawingIcon.FromHandle(grayBitmap.GetHicon());
        }

        private void UpdateTrayIcon(bool isEnabled)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon?.Dispose();
                _notifyIcon.Icon = CreateTrayIcon(isEnabled);
                _notifyIcon.Text = isEnabled
                    ? "Wallpaper Sync"
                    : "Wallpaper Sync (Disabled)";
            }
        }

        private async void StartWallpaperUpdates()
        {
            try
            {
                FileLogger.Log("=== Tray App Starting ===");

                // Show welcome wizard on first run or when launched from the installer.
                // --open-welcome is a safety net: even if state.json somehow survived
                // a reinstall, the installer flag ensures the wizard always shows.
                var state = _appStateService.LoadState();
                bool launchedFromInstaller = Environment.GetCommandLineArgs().Contains("--open-welcome");
                if (state.IsFirstRun || launchedFromInstaller)
                {
                    FileLogger.Log(state.IsFirstRun
                        ? "First run detected - showing welcome wizard"
                        : "--open-welcome flag detected - showing welcome wizard");
                    var wizard = new WelcomeWizard(_configurationService, _appStateService);
                    wizard.ShowDialog();
                    state = _appStateService.LoadState();
                }

                // Check if enabled
                if (!state.IsEnabled)
                {
                    FileLogger.Log("Wallpaper updates are disabled - not starting timer");
                    _status = "Disabled";
                    UpdateTrayIconText();
                    UpdateTrayIcon(false);
                    return;
                }

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
                var imagePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WallpaperSync", "wallpapers");
                if (Directory.Exists(imagePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", imagePath);
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

        private void OnToggleEnabled(object? sender, EventArgs e)
        {
            var state = _appStateService.LoadState();
            state.IsEnabled = !state.IsEnabled;
            _appStateService.SaveState(state);

            if (state.IsEnabled)
            {
                FileLogger.Log("Wallpaper updates enabled by user");
                StartWallpaperUpdates();
                ShowBalloonTip("Wallpaper Sync Enabled",
                    "Automatic updates resumed", ToolTipIcon.Info);
            }
            else
            {
                FileLogger.Log("Wallpaper updates disabled by user");
                StopWallpaperUpdates();
                ShowBalloonTip("Wallpaper Sync Disabled",
                    "Automatic updates paused", ToolTipIcon.Info);
            }

            UpdateTrayIcon(state.IsEnabled);
        }

        private void StopWallpaperUpdates()
        {
            _updateTimer?.Dispose();
            _updateTimer = null;
            _status = "Disabled";
            UpdateTrayIconText();
            FileLogger.Log("Wallpaper updates stopped");
        }

        private void OnToggleStartup(object? sender, EventArgs e)
        {
            try
            {
                if (_startupService.IsStartupEnabled())
                {
                    _startupService.DisableStartup();
                    FileLogger.Log("Startup disabled by user");
                    ShowBalloonTip("Startup Disabled",
                        "Wallpaper Sync will not run at Windows startup", ToolTipIcon.Info);
                }
                else
                {
                    _startupService.EnableStartup();
                    FileLogger.Log("Startup enabled by user");
                    ShowBalloonTip("Startup Enabled",
                        "Wallpaper Sync will run at Windows startup", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                FileLogger.LogError("Failed to toggle startup", ex);
                MessageBox.Show($"Failed to change startup setting: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            _clickTimer?.Stop();
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
