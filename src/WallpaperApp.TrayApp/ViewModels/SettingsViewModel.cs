using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WallpaperApp.Configuration;
using WallpaperApp.Models;
using WallpaperApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace WallpaperApp.TrayApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings window.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IConfigurationService _configService;
        private readonly IAppStateService _stateService;
        private readonly IWallpaperService _wallpaperService;
        private readonly IImageValidator _imageValidator;

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
            }
        }

        private ImageSource _sourceType = ImageSource.Url;
        public ImageSource SourceType
        {
            get => _sourceType;
            set
            {
                _sourceType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsUrlMode));
                OnPropertyChanged(nameof(IsLocalFileMode));
                ValidateUrl();
            }
        }

        public bool IsUrlMode => SourceType == ImageSource.Url;
        public bool IsLocalFileMode => SourceType == ImageSource.LocalFile;

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

            // Load current settings
            LoadCurrentSettings();

            // Initialize commands
            SaveCommand = new RelayCommand(OnSave);
            CancelCommand = new RelayCommand(OnCancel);
            BrowseCommand = new RelayCommand(OnBrowse);
            TestWallpaperCommand = new RelayCommand(OnTestWallpaper);
            ResetToDefaultsCommand = new RelayCommand(OnReset);
            SelectUrlModeCommand = new RelayCommand(() => SourceType = ImageSource.Url);
            SelectLocalFileModeCommand = new RelayCommand(() => SourceType = ImageSource.LocalFile);
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
            }
            catch
            {
                // If loading fails, use defaults
            }
        }

        private void ValidateUrl()
        {
            if (SourceType != ImageSource.Url)
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
            if (!IsUrlValid && SourceType == ImageSource.Url)
            {
                MessageBox.Show("Please fix validation errors before saving.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SourceType == ImageSource.LocalFile && string.IsNullOrWhiteSpace(LocalImagePath))
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

        private void OnTestWallpaper()
        {
            try
            {
                if (SourceType == ImageSource.LocalFile)
                {
                    if (string.IsNullOrWhiteSpace(LocalImagePath))
                    {
                        MessageBox.Show("Please select a local image file first.",
                            "Test Wallpaper", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _wallpaperService.SetWallpaper(LocalImagePath, SelectedFitMode);
                    MessageBox.Show("Wallpaper set successfully!", "Test Wallpaper",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Test wallpaper is only available for local files. Save your settings to test URL mode.",
                        "Test Wallpaper", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set wallpaper: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnReset()
        {
            var result = MessageBox.Show(
                "Reset all settings to defaults?\n\nThis will:\n" +
                "- Clear image URL/path\n" +
                "- Reset refresh interval to 15 minutes\n" +
                "- Set fit mode to Fit\n" +
                "- Clear last-known-good image",
                "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ImageUrl = string.Empty;
                LocalImagePath = string.Empty;
                RefreshIntervalMinutes = 15;
                SelectedFitMode = WallpaperFitMode.Fit;
                SourceType = ImageSource.Url;

                // Clear state (but keep IsFirstRun = false)
                var state = _stateService.LoadState();
                state.LastKnownGoodImagePath = null;
                state.LastUpdateTime = null;
                state.UpdateSuccessCount = 0;
                state.UpdateFailureCount = 0;
                _stateService.SaveState(state);

                MessageBox.Show("Settings reset to defaults.", "Reset Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? CloseWindow { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
