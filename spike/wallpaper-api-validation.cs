using System;
using System.Runtime.InteropServices;

namespace WallpaperSpike
{
    /// <summary>
    /// Tech spike to validate that we can programmatically set desktop wallpaper
    /// using the Windows API via P/Invoke.
    ///
    /// This is throwaway code for validation only - the real implementation
    /// will live in src/WallpaperApp/Services/WallpaperService.cs
    /// </summary>
    class Program
    {
        // P/Invoke declaration for SystemParametersInfo from user32.dll
        // Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint uiAction,
            uint uiParam,
            string pvParam,
            uint fWinIni);

        // Constants from winuser.h
        private const uint SPI_SETDESKWALLPAPER = 0x0014;  // 20 decimal
        private const uint SPIF_UPDATEINIFILE = 0x01;      // Write to user profile
        private const uint SPIF_SENDCHANGE = 0x02;         // Broadcast WM_SETTINGCHANGE

        static void Main(string[] args)
        {
            Console.WriteLine("=== Windows Wallpaper API Validation Spike ===\n");

            // IMPORTANT: Update this path to point to an actual image on your Windows machine
            // Supported formats: BMP, JPG, PNG (Windows 10+)
            string imagePath = @"C:\temp\test-wallpaper.png";

            // Allow override via command-line argument
            if (args.Length > 0)
            {
                imagePath = args[0];
            }

            // Validate file exists
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"❌ ERROR: Image file not found at: {imagePath}");
                Console.WriteLine();
                Console.WriteLine("Usage: WallpaperSpike.exe <path-to-image>");
                Console.WriteLine("   or: Edit Program.cs to update the default imagePath");
                Console.WriteLine();
                Console.WriteLine("Supported formats: BMP, JPG, PNG");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"Image path: {imagePath}");
            Console.WriteLine($"File exists: ✓");
            Console.WriteLine($"File size: {new FileInfo(imagePath).Length:N0} bytes");
            Console.WriteLine();

            // Call the Windows API
            Console.WriteLine("Calling SystemParametersInfo(SPI_SETDESKWALLPAPER)...");

            bool success = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                imagePath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            Console.WriteLine();

            if (success)
            {
                Console.WriteLine("✅ SUCCESS: Wallpaper has been changed!");
                Console.WriteLine();
                Console.WriteLine("Validation checks:");
                Console.WriteLine("  1. Minimize all windows - do you see the new wallpaper?");
                Console.WriteLine("  2. Lock screen (Win+L) and unlock - does wallpaper persist?");
                Console.WriteLine("  3. Open Settings > Personalization > Background - is it updated?");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"❌ FAILED: SystemParametersInfo returned false");
                Console.WriteLine($"   Win32 error code: {errorCode} (0x{errorCode:X})");
                Console.WriteLine();
                Console.WriteLine("Common error codes:");
                Console.WriteLine("  5 (0x5)    = Access Denied - try running as Administrator");
                Console.WriteLine("  87 (0x57)  = Invalid Parameter - check file path format");
                Console.WriteLine("  1813 (0x715) = Invalid image format");
                Environment.Exit(1);
            }
        }
    }
}
