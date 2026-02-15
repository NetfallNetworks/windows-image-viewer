namespace WallpaperApp.Services
{
    /// <summary>
    /// Interface for fetching images from URLs.
    /// </summary>
    public interface IImageFetcher
    {
        /// <summary>
        /// Downloads an image from the specified URL and saves it to a temporary directory.
        /// </summary>
        /// <param name="url">The URL of the image to download.</param>
        /// <returns>The full path to the downloaded file, or null if the download fails.</returns>
        Task<string?> DownloadImageAsync(string url);
    }
}
