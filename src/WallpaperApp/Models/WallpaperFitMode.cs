namespace WallpaperApp.Models
{
    /// <summary>
    /// Wallpaper display modes for how images are positioned/scaled.
    /// These correspond to Windows registry values for WallpaperStyle and TileWallpaper.
    /// </summary>
    public enum WallpaperFitMode
    {
        /// <summary>
        /// Fill screen while maintaining aspect ratio (crops edges if needed).
        /// This is the recommended default for most images.
        /// Registry: WallpaperStyle="10", TileWallpaper="0"
        /// </summary>
        Fill = 0,

        /// <summary>
        /// Fit entire image on screen (letterboxing/pillarboxing if needed).
        /// Registry: WallpaperStyle="6", TileWallpaper="0"
        /// </summary>
        Fit = 1,

        /// <summary>
        /// Stretch image to fill screen (distorts aspect ratio).
        /// Registry: WallpaperStyle="2", TileWallpaper="0"
        /// </summary>
        Stretch = 2,

        /// <summary>
        /// Repeat image as a tiled pattern.
        /// Registry: WallpaperStyle="0", TileWallpaper="1"
        /// </summary>
        Tile = 3,

        /// <summary>
        /// Center image without scaling (shows desktop color around edges).
        /// Registry: WallpaperStyle="0", TileWallpaper="0"
        /// </summary>
        Center = 4
    }
}
