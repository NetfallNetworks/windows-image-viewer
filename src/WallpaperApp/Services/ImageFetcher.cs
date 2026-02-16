namespace WallpaperApp.Services
{
    /// <summary>
    /// Service for downloading images from URLs.
    /// </summary>
    public class ImageFetcher : IImageFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly IImageValidator _imageValidator;
        private readonly string _tempDirectory;

        /// <summary>
        /// Initializes a new instance of ImageFetcher with an HttpClient and ImageValidator.
        /// </summary>
        /// <param name="httpClient">The HttpClient to use for downloads.
        /// Should be configured with a 30-second timeout.</param>
        /// <param name="imageValidator">The validator to check downloaded files.</param>
        public ImageFetcher(HttpClient httpClient, IImageValidator imageValidator)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _imageValidator = imageValidator ?? throw new ArgumentNullException(nameof(imageValidator));

            // Set temp directory to %TEMP%/WeatherWallpaper/
            _tempDirectory = Path.Combine(Path.GetTempPath(), "WeatherWallpaper");
        }

        /// <summary>
        /// Downloads an image from the specified URL and saves it to a temporary directory.
        /// Returns null on any error (no retries).
        /// </summary>
        /// <param name="url">The URL of the image to download.</param>
        /// <returns>The full path to the downloaded file, or null if the download fails.</returns>
        public async Task<string?> DownloadImageAsync(string url)
        {
            try
            {
                // Ensure temp directory exists
                if (!Directory.Exists(_tempDirectory))
                {
                    Directory.CreateDirectory(_tempDirectory);
                }

                // Download the image
                var response = await _httpClient.GetAsync(url);

                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    LogError($"HTTP error downloading image from {url}: {response.StatusCode}");
                    return null;
                }

                // Generate unique filename based on timestamp
                string filename = GenerateUniqueFilename();
                string fullPath = Path.Combine(_tempDirectory, filename);

                // Save the image to disk
                using (var fileStream = File.Create(fullPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                // Validate the downloaded file (Security Fix - Story WS-1)
                if (!_imageValidator.IsValidImage(fullPath, out var format))
                {
                    LogError($"Downloaded file failed validation: {url}");
                    File.Delete(fullPath); // Delete invalid file
                    return null;
                }

                LogInformation($"Downloaded valid {format} image from {url} to {fullPath}");
                return fullPath;
            }
            catch (TaskCanceledException)
            {
                // Timeout occurred
                LogWarning($"Timeout downloading image from {url}");
                return null;
            }
            catch (HttpRequestException ex)
            {
                LogWarning($"HTTP request error downloading image from {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                LogWarning($"Error downloading image from {url}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a unique filename based on the current timestamp with millisecond precision.
        /// Format: wallpaper-{yyyyMMdd-HHmmss-fff}.png
        /// </summary>
        private string GenerateUniqueFilename()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            return $"wallpaper-{timestamp}.png";
        }

        /// <summary>
        /// Logs an information message.
        /// </summary>
        private void LogInformation(string message)
        {
            // TODO: Replace with proper logging in Story 8
            Console.WriteLine($"[INFO] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        private void LogWarning(string message)
        {
            // TODO: Replace with proper logging in Story 8
            Console.WriteLine($"[WARNING] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        private void LogError(string message)
        {
            // TODO: Replace with proper logging in Story 8
            Console.WriteLine($"[ERROR] {message}");
        }
    }
}
