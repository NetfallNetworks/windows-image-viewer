// WallpaperApp.WidgetProvider - Windows 11 Widget Board COM server
// Requires: Windows App SDK 1.6+, Windows 11 Build 22621+
//
// Launch model: The Widget Board host process activates this executable via COM
// when the user opens Win+W. This process registers as a COM class object and
// blocks until the Widget Board releases it.
//
// Manual registration for development testing:
//   1. Build in Release: dotnet build -c Release
//   2. Register COM CLSID in HKCU\Software\Classes\CLSID\{<CLSID>}\LocalServer32
//   3. Run WallpaperApp.WidgetProvider.exe
//   4. Open Win+W to trigger activation

using Microsoft.Windows.ApplicationModel;
using Microsoft.Windows.Widgets.Providers;
using WallpaperApp.Configuration;
using WallpaperApp.Services;

// Ensure Windows App Runtime is installed before any SDK calls
WindowsAppRuntime.EnsureIsInstalled();

// Wire up Core services (no DI container — keep the provider lightweight)
var configService = new ConfigurationService();
var stateService = new AppStateService();
var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var imageValidator = new ImageValidator();
var imageFetcher = new ImageFetcher(httpClient, imageValidator);
var wallpaperService = new WallpaperService();
var fileCleanup = new FileCleanupService();
var updater = new WallpaperUpdater(configService, imageFetcher, wallpaperService, stateService, fileCleanup);

// Create the widget provider
var provider = new WallpaperImageWidgetProvider(configService, stateService, updater);

// Register as COM class object and block until Widget Board releases this process.
// WidgetProviderServer<T> handles CoRegisterClassObject / CoRevokeClassObject
// and the blocking wait internally.
using var server = new WidgetProviderServer<WallpaperImageWidgetProvider>();
server.Run(provider);
