namespace WallpaperApp.Widget
{
    /// <summary>
    /// Abstraction over the Windows <c>PackageManager</c> WinRT API.
    /// Allows <see cref="WidgetPackageRegistrar"/> to be unit-tested without
    /// depending on real WinRT interop (which only works on Windows).
    /// </summary>
    public interface IPackageManagerAdapter
    {
        /// <summary>
        /// Returns <c>true</c> if a package with the given family name prefix
        /// is already registered for the current user.
        /// </summary>
        bool IsPackageRegistered(string packageFamilyNamePrefix);

        /// <summary>
        /// Registers a sparse MSIX identity package, pointing to the external
        /// content directory where the executable is installed.
        /// </summary>
        /// <param name="msixPath">Absolute path to the .msix file on disk.</param>
        /// <param name="externalLocationUri">
        /// Directory containing the externally-installed executable referenced
        /// by the manifest's <c>Executable</c> attribute.
        /// </param>
        /// <returns><c>true</c> if registration succeeded.</returns>
        Task<bool> RegisterSparsePackageAsync(string msixPath, string externalLocationUri);

        /// <summary>
        /// Removes a registered package whose family name starts with the given prefix.
        /// </summary>
        /// <param name="packageFamilyNamePrefix">Prefix to match against installed package family names.</param>
        /// <returns><c>true</c> if removal succeeded or no matching package was found.</returns>
        Task<bool> RemovePackageAsync(string packageFamilyNamePrefix);
    }
}
