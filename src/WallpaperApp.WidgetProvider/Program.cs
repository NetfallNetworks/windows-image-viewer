// WallpaperApp.WidgetProvider - Windows 11 Widget Board COM server
// Requires: Windows App SDK 1.6+, Windows 11 Build 22621+
//
// Launch model: The Widget Board host process activates this executable via COM
// when the user opens Win+W. This process registers as a COM class object and
// blocks until the Widget Board releases it.
//
// Manual registration for development testing:
//   1. Build in Release: dotnet build -c Release
//   2. Register COM CLSID in HKCU\Software\Classes\CLSID\{6B2A4C8E-3F1D-4A9B-8E2C-5D7F1A3B6E4C}\LocalServer32
//      with the full path to WallpaperApp.WidgetProvider.exe
//   3. Run WallpaperApp.WidgetProvider.exe
//   4. Open Win+W to trigger activation

using System.Runtime.InteropServices;
using WallpaperApp.Configuration;
using WallpaperApp.Widget;
using WallpaperApp.WidgetProvider;
using WallpaperApp.Services;
using WinRT;

// Initialize CsWinRT ComWrappers — required before any WinRT interface marshaling.
// Without this, the Widget Board cannot communicate with the provider through COM.
ComWrappersSupport.InitializeComWrappers();

// COM constants — declared before use
const uint CLSCTX_LOCAL_SERVER = 4;
const uint REGCLS_MULTIPLEUSE = 1;

// Wire up Core services (no DI container — keep the provider lightweight)
var configService = new ConfigurationService();
var stateService = new AppStateService();
var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var imageValidator = new ImageValidator();
var imageFetcher = new ImageFetcher(httpClient, imageValidator);
var wallpaperService = new WallpaperService(imageValidator);
var fileCleanup = new FileCleanupService();
var updater = new WallpaperUpdater(configService, imageFetcher, wallpaperService, stateService, fileCleanup);

// Create the widget provider instance
var provider = new WallpaperImageWidgetProvider(configService, stateService, updater);

// Start the IPC listener so TrayApp can signal instant widget refresh after wallpaper updates.
// The WidgetIpcService creates the named EventWaitHandle and calls PushUpdateToAllWidgets()
// on the provider whenever the handle is signaled.
using var ipcService = new WidgetIpcService(provider.PushUpdateToAllWidgets);
Console.WriteLine("[WidgetProvider] IPC listener started.");

// Register as COM class object so the Widget Board host can activate this process.
// CoRegisterClassObject tells the COM SCM "I am the server for CLSID {6B2A4C8E-...}".
var clsid = typeof(WallpaperImageWidgetProvider).GUID;
var factory = new WidgetProviderClassFactory(provider);
int hr = CoRegisterClassObject(ref clsid, factory, CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, out uint cookie);
Marshal.ThrowExceptionForHR(hr);

Console.WriteLine($"[WidgetProvider] Registered COM server (CLSID {clsid}). Waiting for Widget Board activation...");
Console.WriteLine("[WidgetProvider] Press Ctrl+C to exit.");

// Block until Ctrl+C or the Widget Board signals us to exit
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
cts.Token.WaitHandle.WaitOne();

// Clean up COM registration
CoRevokeClassObject(cookie);
Console.WriteLine("[WidgetProvider] COM server unregistered. Exiting.");

return 0;

// ── COM P/Invoke declarations ────────────────────────────────────────────────

[DllImport("ole32.dll")]
static extern int CoRegisterClassObject(
    ref Guid rclsid,
    [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
    uint dwClsContext,
    uint flags,
    out uint lpdwRegister);

[DllImport("ole32.dll")]
static extern int CoRevokeClassObject(uint dwRegister);
