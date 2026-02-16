using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for managing Windows startup configuration.
    /// </summary>
    public class StartupService : IStartupService
    {
        private const string APP_NAME = "Wallpaper Sync";

        /// <summary>
        /// Checks if the application is configured to run at Windows startup.
        /// </summary>
        /// <returns>True if startup is enabled, false otherwise.</returns>
        public bool IsStartupEnabled()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");
            return File.Exists(shortcutPath);
        }

        /// <summary>
        /// Enables the application to run at Windows startup.
        /// Creates a shortcut in the Startup folder.
        /// </summary>
        public void EnableStartup()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");
            string targetPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new InvalidOperationException("Could not determine application path");
            }

            // Create shortcut using IWshRuntimeLibrary (Windows Script Host)
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    throw new InvalidOperationException("Could not create WScript.Shell object");
                }

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Description = "Wallpaper Sync - Automatic wallpaper updates";
                shortcut.Save();

                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create startup shortcut: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disables the application from running at Windows startup.
        /// Removes the shortcut from the Startup folder.
        /// </summary>
        public void DisableStartup()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, $"{APP_NAME}.lnk");

            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }
    }
}
