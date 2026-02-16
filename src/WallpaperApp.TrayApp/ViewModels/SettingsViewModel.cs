using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperApp.Configuration;
using WallpaperApp.Models;
using WallpaperApp.Services;
using MessageBox = System.Windows.MessageBox;
using WpfImageSource = System.Windows.Media.ImageSource;
using ModelImageSource = WallpaperApp.Models.ImageSource;

namespace WallpaperApp.TrayApp.ViewModels
{
    /// <summary>
    /// Represents a preset time interval option.
    /// </summary>
    public class IntervalPreset
    {
        public string DisplayName { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public bool IsCustom { get; set; }
    }

    /// <summary>
    /// ViewModel for the Settings window.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IConfigurationService _configService;
        private readonly IAppStateService _stateService;
        private readonly IWallpaperService _wallpaperService;
        private readonly IImageValidator _imageValidator;
        private readonly HttpClient _httpClient;
        private System.Threading.Timer? _previewDebounceTimer;

        // Original values for undo functionality
        private string _originalImageUrl = string.Empty;
        private string _originalLocalImagePath = string.Empty;
        private ModelImageSource _originalSourceType;
        private int _originalRefreshIntervalMinutes;
        private WallpaperFitMode _originalSelectedFitMode;

        // Bindable Properties
        private string _imageUrl = string.Empty;
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                _imageUrl = value;
                OnPropertyChanged();
                ValidateUrl();
                SchedulePreviewUpdate();
            }
        }

        private string _localImagePath = string.Empty;
        public string LocalImagePath
        {
            get => _localImagePath;
            set
            {
                _localImagePath = value;
                OnPropertyChanged();
                SchedulePreviewUpdate();
            }
        }

        private ModelImageSource _sourceType = ModelImageSource.Url;
        public ModelImageSource SourceType
        {
            get => _sourceType;
            set
            {
                _sourceType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUrlMode));
                OnPropertyChanged(nameof(IsLocalFileMode));
                ValidateUrl();
                SchedulePreviewUpdate();
            }
        }

        public bool IsUrlMode
        {
            get => SourceType == ModelImageSource.Url;
            set { if (value) SourceType = ModelImageSource.Url; }
        }

        public bool IsLocalFileMode
        {
            get => SourceType == ModelImageSource.LocalFile;
            set { if (value) SourceType = ModelImageSource.LocalFile; }
        }

        private int _refreshIntervalMinutes = 15;
        public int RefreshIntervalMinutes
        {
            get => _refreshIntervalMinutes;
            set
            {
                _refreshIntervalMinutes = Math.Clamp(value, 1, 1440);
                OnPropertyChanged();
            }
        }

        private WallpaperFitMode _selectedFitMode = WallpaperFitMode.Fit;
        public WallpaperFitMode SelectedFitMode
        {
            get => _selectedFitMode;
            set
            {
                _selectedFitMode = value;
                OnPropertyChanged();
                UpdatePreviewStretch();
                SchedulePreviewUpdate();
            }
        }

        private void UpdatePreviewStretch()
        {
            // For Center and Tile modes, we use an ImageBrush instead of Image.Stretch
            UseImageBrush = _selectedFitMode == WallpaperFitMode.Center || _selectedFitMode == WallpaperFitMode.Tile;

            if (!UseImageBrush)
            {
                // For Fit, Fill, Stretch - use regular Image element with Stretch
                PreviewStretch = _selectedFitMode switch
                {
                    WallpaperFitMode.Fit => System.Windows.Media.Stretch.Uniform,
                    WallpaperFitMode.Fill => System.Windows.Media.Stretch.UniformToFill,
                    WallpaperFitMode.Stretch => System.Windows.Media.Stretch.Fill,
                    _ => System.Windows.Media.Stretch.Uniform
                };
                PreviewBrush = null;
            }
            else
            {
                // For Center and Tile - create ImageBrush with appropriate settings
                UpdatePreviewBrush();
            }
        }

        private void UpdatePreviewBrush()
        {
            if (PreviewImage == null)
            {
                PreviewBrush = null;
                return;
            }

            var imageBrush = new System.Windows.Media.ImageBrush(PreviewImage);

            if (_selectedFitMode == WallpaperFitMode.Center)
            {
                // Center mode: Show image at scaled size, centered, no tiling
                imageBrush.Stretch = System.Windows.Media.Stretch.None;
                imageBrush.AlignmentX = System.Windows.Media.AlignmentX.Center;
                imageBrush.AlignmentY = System.Windows.Media.AlignmentY.Center;
                imageBrush.TileMode = System.Windows.Media.TileMode.None;

                // Scale the image down to match preview scale
                imageBrush.Transform = new System.Windows.Media.ScaleTransform(PreviewScale, PreviewScale);
            }
            else if (_selectedFitMode == WallpaperFitMode.Tile)
            {
                // Tile mode: Show image at scaled size, tiled to fill
                imageBrush.Stretch = System.Windows.Media.Stretch.None;
                imageBrush.TileMode = System.Windows.Media.TileMode.Tile;

                // Calculate viewport size (tile size) based on image dimensions and preview scale
                if (PreviewImage is BitmapSource bitmap)
                {
                    double scaledWidth = bitmap.PixelWidth * PreviewScale;
                    double scaledHeight = bitmap.PixelHeight * PreviewScale;

                    imageBrush.Viewport = new System.Windows.Rect(0, 0, scaledWidth, scaledHeight);
                    imageBrush.ViewportUnits = System.Windows.Media.BrushMappingMode.Absolute;
                }
            }

            PreviewBrush = imageBrush;
        }

        // Interval presets
        public ObservableCollection<IntervalPreset> IntervalPresets { get; }

        private IntervalPreset? _selectedIntervalPreset;
        public IntervalPreset? SelectedIntervalPreset
        {
            get => _selectedIntervalPreset;
            set
            {
                _selectedIntervalPreset = value;
                OnPropertyChanged();
                if (value != null && !value.IsCustom)
                {
                    RefreshIntervalMinutes = value.Minutes;
                }
                OnPropertyChanged(nameof(IsCustomIntervalSelected));
            }
        }

        public bool IsCustomIntervalSelected => _selectedIntervalPreset?.IsCustom ?? false;

        private string _customIntervalValue = "30";
        public string CustomIntervalValue
        {
            get => _customIntervalValue;
            set
            {
                _customIntervalValue = value;
                OnPropertyChanged();
                UpdateCustomInterval();
            }
        }

        private string _customIntervalUnit = "Minutes";
        public string CustomIntervalUnit
        {
            get => _customIntervalUnit;
            set
            {
                _customIntervalUnit = value;
                OnPropertyChanged();
                UpdateCustomInterval();
            }
        }

        // Preview properties
        private WpfImageSource? _previewImage;
        public WpfImageSource? PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged();
            }
        }

        private string _previewStatus = "Select an image to preview";
        public string PreviewStatus
        {
            get => _previewStatus;
            set
            {
                _previewStatus = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Media.Stretch _previewStretch = System.Windows.Media.Stretch.Uniform;
        public System.Windows.Media.Stretch PreviewStretch
        {
            get => _previewStretch;
            set
            {
                _previewStretch = value;
                OnPropertyChanged();
            }
        }

        private double _screenWidth;
        public double ScreenWidth
        {
            get => _screenWidth;
            set
            {
                _screenWidth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewWidth));
                OnPropertyChanged(nameof(PreviewHeight));
            }
        }

        private double _screenHeight;
        public double ScreenHeight
        {
            get => _screenHeight;
            set
            {
                _screenHeight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PreviewWidth));
                OnPropertyChanged(nameof(PreviewHeight));
            }
        }

        // Calculate preview dimensions to match screen aspect ratio
        public double PreviewWidth
        {
            get
            {
                const double maxWidth = 620.0;
                return maxWidth; // Always use max width, height adjusts to maintain aspect ratio
            }
        }

        public double PreviewHeight
        {
            get
            {
                const double maxWidth = 620.0;
                if (ScreenWidth <= 0 || ScreenHeight <= 0) return 350.0;

                double aspectRatio = ScreenWidth / ScreenHeight;
                return maxWidth / aspectRatio;
            }
        }

        // Calculate scale factor for preview (how much smaller the preview is compared to real screen)
        public double PreviewScale
        {
            get
            {
                if (ScreenWidth <= 0) return 1.0;
                return PreviewWidth / ScreenWidth;
            }
        }

        private bool _useImageBrush;
        public bool UseImageBrush
        {
            get => _useImageBrush;
            set
            {
                _useImageBrush = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Media.Brush? _previewBrush;
        public System.Windows.Media.Brush? PreviewBrush
        {
            get => _previewBrush;
            set
            {
                _previewBrush = value;
                OnPropertyChanged();
            }
        }

        private string? _urlValidationError;
        public string? UrlValidationError
        {
            get => _urlValidationError;
            set
            {
                _urlValidationError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUrlValid));
            }
        }

        public bool IsUrlValid => string.IsNullOrEmpty(UrlValidationError);

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseCommand { get; }
        public ICommand TestWallpaperCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }
        public ICommand SelectUrlModeCommand { get; }
        public ICommand SelectLocalFileModeCommand { get; }

        public SettingsViewModel(
            IConfigurationService configService,
            IAppStateService stateService,
            IWallpaperService wallpaperService,
            IImageValidator imageValidator)
        {
            _configService = configService;
            _stateService = stateService;
            _wallpaperService = wallpaperService;
            _imageValidator = imageValidator;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Initialize interval presets
            IntervalPresets = new ObservableCollection<IntervalPreset>
            {
                new IntervalPreset { DisplayName = "5 minutes", Minutes = 5 },
                new IntervalPreset { DisplayName = "10 minutes", Minutes = 10 },
                new IntervalPreset { DisplayName = "15 minutes", Minutes = 15 },
                new IntervalPreset { DisplayName = "30 minutes", Minutes = 30 },
                new IntervalPreset { DisplayName = "1 hour", Minutes = 60 },
                new IntervalPreset { DisplayName = "2 hours", Minutes = 120 },
                new IntervalPreset { DisplayName = "4 hours", Minutes = 240 },
                new IntervalPreset { DisplayName = "6 hours", Minutes = 360 },
                new IntervalPreset { DisplayName = "8 hours", Minutes = 480 },
                new IntervalPreset { DisplayName = "12 hours", Minutes = 720 },
                new IntervalPreset { DisplayName = "18 hours", Minutes = 1080 },
                new IntervalPreset { DisplayName = "24 hours", Minutes = 1440 },
                new IntervalPreset { DisplayName = "Custom...", Minutes = 30, IsCustom = true }
            };

            // Get screen resolution
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                ScreenWidth = primaryScreen.Bounds.Width;
                ScreenHeight = primaryScreen.Bounds.Height;
            }

            // Load current settings
            LoadCurrentSettings();

            // Initialize preview stretch mode based on current fit mode
            UpdatePreviewStretch();

            // Save original values for undo
            SaveOriginalValues();

            // Initialize commands
            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(OnCancel);
            BrowseCommand = new RelayCommand(OnBrowse);
            TestWallpaperCommand = new RelayCommand(async () => await OnTestWallpaperAsync());
            ResetToDefaultsCommand = new RelayCommand(OnUndoChanges);
            SelectUrlModeCommand = new RelayCommand(() => SourceType = ModelImageSource.Url);
            SelectLocalFileModeCommand = new RelayCommand(() => SourceType = ModelImageSource.LocalFile);

            // Initial preview load
            _ = UpdatePreviewAsync();
        }

        private void SaveOriginalValues()
        {
            _originalImageUrl = _imageUrl;
            _originalLocalImagePath = _localImagePath;
            _originalSourceType = _sourceType;
            _originalRefreshIntervalMinutes = _refreshIntervalMinutes;
            _originalSelectedFitMode = _selectedFitMode;
        }

        private void LoadCurrentSettings()
        {
            try
            {
                var settings = _configService.LoadConfiguration();
                ImageUrl = settings.ImageUrl ?? string.Empty;
                LocalImagePath = settings.LocalImagePath ?? string.Empty;
                SourceType = settings.SourceType;
                RefreshIntervalMinutes = settings.RefreshIntervalMinutes;
                SelectedFitMode = settings.FitMode;

                // Select the matching interval preset
                var matchingPreset = IntervalPresets.FirstOrDefault(p => !p.IsCustom && p.Minutes == RefreshIntervalMinutes);
                if (matchingPreset != null)
                {
                    SelectedIntervalPreset = matchingPreset;
                }
                else
                {
                    // Use custom
                    SelectedIntervalPreset = IntervalPresets.Last(); // Custom option
                    CustomIntervalValue = RefreshIntervalMinutes.ToString();
                    CustomIntervalUnit = "Minutes";
                }
            }
            catch
            {
                // If loading fails, use defaults
                SelectedIntervalPreset = IntervalPresets.FirstOrDefault(p => p.Minutes == 15);
            }
        }

        private void UpdateCustomInterval()
        {
            if (SelectedIntervalPreset?.IsCustom ?? false)
            {
                if (int.TryParse(CustomIntervalValue, out int value) && value >= 1)
                {
                    int minutes = CustomIntervalUnit == "Hours" ? value * 60 : value;
                    // Enforce minimum of 1 minute
                    RefreshIntervalMinutes = Math.Max(1, minutes);
                }
            }
        }

        private void ValidateUrl()
        {
            if (SourceType != ModelImageSource.Url)
            {
                UrlValidationError = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(ImageUrl))
            {
                UrlValidationError = "URL is required";
                return;
            }

            if (!ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                UrlValidationError = "Must use HTTPS";
                return;
            }

            if (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out _))
            {
                UrlValidationError = "Invalid URL format";
                return;
            }

            UrlValidationError = null;
        }

        private void OnSave()
        {
            ValidateUrl();
            if (!IsUrlValid && SourceType == ModelImageSource.Url)
            {
                MessageBox.Show("Please fix validation errors before saving.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SourceType == ModelImageSource.LocalFile && string.IsNullOrWhiteSpace(LocalImagePath))
            {
                MessageBox.Show("Please select a local image file.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = new AppSettings
            {
                ImageUrl = ImageUrl,
                LocalImagePath = LocalImagePath,
                SourceType = SourceType,
                RefreshIntervalMinutes = RefreshIntervalMinutes,
                FitMode = SelectedFitMode
            };

            try
            {
                // Save to JSON
                _configService.SaveConfiguration(settings);

                MessageBox.Show("Settings saved successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Close window
                CloseWindow?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCancel()
        {
            CloseWindow?.Invoke();
        }

        private void OnBrowse()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Select Wallpaper Image"
            };

            if (dialog.ShowDialog() == true)
            {
                // Validate using ImageValidator
                if (_imageValidator.IsValidImage(dialog.FileName, out var format))
                {
                    LocalImagePath = dialog.FileName;
                }
                else
                {
                    MessageBox.Show(
                        "The selected file is not a valid image. Only PNG, JPG, and BMP are supported.",
                        "Invalid Image", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task OnTestWallpaperAsync()
        {
            try
            {
                string? imagePath = null;
                bool isTempFile = false;

                if (SourceType == ModelImageSource.LocalFile)
                {
                    if (string.IsNullOrWhiteSpace(LocalImagePath))
                    {
                        MessageBox.Show("Please select a local image file first.",
                            "Preview Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    imagePath = LocalImagePath;
                }
                else // URL mode
                {
                    ValidateUrl();
                    if (!IsUrlValid)
                    {
                        MessageBox.Show("Please enter a valid HTTPS URL first.",
                            "Preview Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Download the image temporarily
                    PreviewStatus = "Downloading image...";
                    try
                    {
                        var response = await _httpClient.GetAsync(ImageUrl);
                        if (!response.IsSuccessStatusCode)
                        {
                            MessageBox.Show($"Failed to download image: {response.StatusCode}",
                                "Preview Image", MessageBoxButton.OK, MessageBoxImage.Error);
                            PreviewStatus = "Download failed";
                            return;
                        }

                        var tempPath = Path.Combine(Path.GetTempPath(), "WallpaperPreview", $"preview_{DateTime.Now:yyyyMMddHHmmss}.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                        using (var fileStream = File.Create(tempPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        if (!_imageValidator.IsValidImage(tempPath, out var format))
                        {
                            File.Delete(tempPath);
                            MessageBox.Show("The URL does not contain a valid image.",
                                "Preview Image", MessageBoxButton.OK, MessageBoxImage.Error);
                            PreviewStatus = "Invalid image";
                            return;
                        }

                        imagePath = tempPath;
                        isTempFile = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to download image: {ex.Message}",
                            "Preview Image", MessageBoxButton.OK, MessageBoxImage.Error);
                        PreviewStatus = "Download failed";
                        return;
                    }
                }

                // Set the wallpaper
                _wallpaperService.SetWallpaper(imagePath, SelectedFitMode);
                MessageBox.Show("Wallpaper applied successfully!", "Preview Image",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                PreviewStatus = "Ready";

                // Clean up temp file
                if (isTempFile && imagePath != null && File.Exists(imagePath))
                {
                    try { File.Delete(imagePath); } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set wallpaper: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PreviewStatus = "Error";
            }
        }

        private void OnUndoChanges()
        {
            var result = MessageBox.Show(
                "Undo all changes since opening this window?",
                "Undo Changes", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ImageUrl = _originalImageUrl;
                LocalImagePath = _originalLocalImagePath;
                SourceType = _originalSourceType;
                RefreshIntervalMinutes = _originalRefreshIntervalMinutes;
                SelectedFitMode = _originalSelectedFitMode;

                // Select the matching interval preset
                var matchingPreset = IntervalPresets.FirstOrDefault(p => !p.IsCustom && p.Minutes == RefreshIntervalMinutes);
                if (matchingPreset != null)
                {
                    SelectedIntervalPreset = matchingPreset;
                }
                else
                {
                    SelectedIntervalPreset = IntervalPresets.Last();
                    CustomIntervalValue = RefreshIntervalMinutes.ToString();
                    CustomIntervalUnit = "Minutes";
                }

                _ = UpdatePreviewAsync();
            }
        }

        private void SchedulePreviewUpdate()
        {
            // Cancel existing timer
            _previewDebounceTimer?.Dispose();

            // Schedule new update after 1000ms
            _previewDebounceTimer = new System.Threading.Timer(
                async _ => await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await UpdatePreviewAsync()),
                null,
                1000, // 1 second debounce
                System.Threading.Timeout.Infinite);
        }

        private async Task UpdatePreviewAsync()
        {
            try
            {
                string? imagePath = null;
                bool isTempFile = false;

                if (SourceType == ModelImageSource.LocalFile && !string.IsNullOrWhiteSpace(LocalImagePath))
                {
                    if (File.Exists(LocalImagePath))
                    {
                        imagePath = LocalImagePath;
                        PreviewStatus = $"Preview: {Path.GetFileName(LocalImagePath)}";
                    }
                    else
                    {
                        PreviewStatus = "File not found";
                        PreviewImage = null;
                        return;
                    }
                }
                else if (SourceType == ModelImageSource.Url && !string.IsNullOrWhiteSpace(ImageUrl))
                {
                    ValidateUrl();
                    if (!IsUrlValid)
                    {
                        PreviewStatus = "Invalid URL";
                        PreviewImage = null;
                        return;
                    }

                    PreviewStatus = "Loading preview...";
                    try
                    {
                        var response = await _httpClient.GetAsync(ImageUrl);
                        if (!response.IsSuccessStatusCode)
                        {
                            PreviewStatus = "Download failed";
                            PreviewImage = null;
                            return;
                        }

                        var tempPath = Path.Combine(Path.GetTempPath(), "WallpaperPreview", $"preview_{DateTime.Now:yyyyMMddHHmmss}.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

                        using (var fileStream = File.Create(tempPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }

                        if (!_imageValidator.IsValidImage(tempPath, out var format))
                        {
                            File.Delete(tempPath);
                            PreviewStatus = "Invalid image";
                            PreviewImage = null;
                            return;
                        }

                        imagePath = tempPath;
                        isTempFile = true;
                        PreviewStatus = $"Preview: {format} image from URL";
                    }
                    catch
                    {
                        PreviewStatus = "Failed to load preview";
                        PreviewImage = null;
                        return;
                    }
                }
                else
                {
                    PreviewStatus = "Select an image to preview";
                    PreviewImage = null;
                    return;
                }

                // Load the image
                if (imagePath != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;

                    // Update brush for Center/Tile modes
                    if (UseImageBrush)
                    {
                        UpdatePreviewBrush();
                    }

                    // Clean up temp file
                    if (isTempFile)
                    {
                        try { File.Delete(imagePath); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                PreviewStatus = $"Error: {ex.Message}";
                PreviewImage = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? CloseWindow { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _previewDebounceTimer?.Dispose();
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Simple relay command implementation for WPF commanding.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}
