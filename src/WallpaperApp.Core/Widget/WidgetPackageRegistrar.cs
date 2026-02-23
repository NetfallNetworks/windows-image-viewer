using WallpaperApp.Services;

namespace WallpaperApp.Widget
{
    /// <summary>
    /// Registers the sparse MSIX identity package on first run so the
    /// Widget Board can activate the widget provider COM server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registrar checks whether the identity package is already registered;
    /// if not, it calls <see cref="IPackageManagerAdapter.RegisterSparsePackageAsync"/>
    /// to register it. Registration is idempotent — calling it when already registered
    /// is a no-op.
    /// </para>
    /// <para>
    /// The MSIX file is expected at <c>{AppContext.BaseDirectory}\WallpaperSync-Identity.msix</c>.
    /// If the file is missing, registration is skipped with a warning log.
    /// </para>
    /// </remarks>
    public class WidgetPackageRegistrar
    {
        private readonly IPackageManagerAdapter _packageManager;

        /// <summary>
        /// Package family name prefix used to check if the identity package is installed.
        /// </summary>
        public const string PackageFamilyNamePrefix = "WallpaperSync.WidgetProvider";

        /// <summary>
        /// Expected MSIX file path (relative to the application base directory).
        /// The MSIX is installed to the widget\ subdirectory by the MSI installer.
        /// </summary>
        public const string MsixRelativePath = @"widget\WallpaperSync-Identity.msix";

        /// <summary>
        /// Widget provider subdirectory name (relative to the application base directory).
        /// The ExternalLocationUri must point here so the sparse MSIX can resolve
        /// the Executable path in AppxManifest.xml.
        /// </summary>
        public const string WidgetProviderSubdir = "WidgetProvider";

        public WidgetPackageRegistrar(IPackageManagerAdapter packageManager)
        {
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
        }

        /// <summary>
        /// Registers the sparse MSIX identity package if it is not already registered.
        /// Safe to call multiple times — skips registration if the package is already present.
        /// </summary>
        /// <returns><c>true</c> if registration was performed or already present; <c>false</c> on error.</returns>
        public async Task<bool> RegisterIfNeededAsync()
        {
            try
            {
                // Check if already registered
                if (_packageManager.IsPackageRegistered(PackageFamilyNamePrefix))
                {
                    FileLogger.Log("[WidgetPackageRegistrar] Identity package already registered, skipping.");
                    return true;
                }

                // Locate the MSIX file in the widget\ subdirectory
                var msixPath = Path.Combine(AppContext.BaseDirectory, MsixRelativePath);
                if (!File.Exists(msixPath))
                {
                    FileLogger.Log($"[WidgetPackageRegistrar] MSIX not found at {msixPath}, skipping registration.");
                    return false;
                }

                // ExternalLocationUri must point to the WidgetProvider\ subdirectory
                // where the exe and its DLLs are installed
                var externalLocation = Path.Combine(AppContext.BaseDirectory, WidgetProviderSubdir);
                FileLogger.Log($"[WidgetPackageRegistrar] Registering identity package from {msixPath}...");
                var result = await _packageManager.RegisterSparsePackageAsync(
                    msixPath,
                    externalLocation);

                if (result)
                {
                    FileLogger.Log("[WidgetPackageRegistrar] Identity package registered successfully.");
                }
                else
                {
                    FileLogger.Log("[WidgetPackageRegistrar] Identity package registration returned false.");
                }

                return result;
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[WidgetPackageRegistrar] Registration failed", ex);
                return false;
            }
        }
    }
}
