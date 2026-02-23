using WallpaperApp.Widget;
using WallpaperApp.Services;

namespace WallpaperApp.TrayApp.Services
{
    /// <summary>
    /// Windows-specific implementation of <see cref="IPackageManagerAdapter"/> using
    /// the <c>Windows.Management.Deployment.PackageManager</c> WinRT API.
    /// </summary>
    /// <remarks>
    /// This adapter wraps the WinRT PackageManager so the registrar logic can be
    /// unit-tested with a mock. The real PackageManager requires Windows 10 2004+.
    /// </remarks>
    public class WindowsPackageManagerAdapter : IPackageManagerAdapter
    {
        /// <inheritdoc/>
        public bool IsPackageRegistered(string packageFamilyNamePrefix)
        {
            try
            {
                var packageManager = new Windows.Management.Deployment.PackageManager();
                var packages = packageManager.FindPackagesForUser("");

                foreach (var package in packages)
                {
                    if (package.Id.FamilyName.StartsWith(packageFamilyNamePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[PackageManagerAdapter] Error checking package registration", ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterSparsePackageAsync(string msixPath, string externalLocationUri)
        {
            try
            {
                var packageManager = new Windows.Management.Deployment.PackageManager();
                var msixUri = new Uri(msixPath);
                var externalUri = new Uri(externalLocationUri);

                var options = new Windows.Management.Deployment.AddPackageOptions
                {
                    ExternalLocationUri = externalUri
                };

                var operation = packageManager.AddPackageByUriAsync(msixUri, options);
                var result = await operation.AsTask();

                if (!string.IsNullOrEmpty(result.ErrorText))
                {
                    FileLogger.Log($"[PackageManagerAdapter] Registration error: {result.ErrorText}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[PackageManagerAdapter] Registration failed", ex);
                return false;
            }
        }
    }
}
