namespace WallpaperApp.Widget
{
    /// <summary>
    /// Shared constants for IPC between the TrayApp and Widget Provider processes.
    /// Both processes reference this class so the named EventWaitHandle uses
    /// the same name without hardcoded strings on either side.
    /// </summary>
    public static class WidgetIpcConstants
    {
        /// <summary>
        /// Named EventWaitHandle used to signal the widget provider that a wallpaper
        /// update has occurred. The <c>Global\</c> prefix makes the handle accessible
        /// across Windows session boundaries.
        /// </summary>
        public const string WidgetRefreshEventName = @"Global\WallpaperSyncWidgetRefresh";
    }
}
