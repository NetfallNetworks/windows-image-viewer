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
    /// ViewModel for the Welcome Wizard.
    /// </summary>
    public class WelcomeViewModel : INotifyPropertyChanged
    {
        private readonly IConfigurationService _configService;
        private readonly IAppStateService _stateService;

        private int _currentPage = 0;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPage1));
                OnPropertyChanged(nameof(IsPage2));
            }
        }

        public bool IsPage1 => CurrentPage == 0;
        public bool IsPage2 => CurrentPage == 1;

        private string _demoUrl = "https://source.unsplash.com/random/1920x1080";
        public string DemoUrl
        {
            get => _demoUrl;
            set
            {
                _demoUrl = value;
                OnPropertyChanged();
            }
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

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand FinishCommand { get; }

        public WelcomeViewModel(IConfigurationService configService, IAppStateService stateService)
        {
            _configService = configService;
            _stateService = stateService;

            NextCommand = new RelayCommand(OnNext, () => CurrentPage < 1);
            BackCommand = new RelayCommand(OnBack, () => CurrentPage > 0);
            FinishCommand = new RelayCommand(OnFinish);
        }

        private void OnNext()
        {
            if (CurrentPage < 1)
                CurrentPage++;
        }

        private void OnBack()
        {
            if (CurrentPage > 0)
                CurrentPage--;
        }

        private void OnFinish()
        {
            try
            {
                // Save settings
                var settings = new AppSettings
                {
                    ImageUrl = DemoUrl,
                    RefreshIntervalMinutes = RefreshIntervalMinutes,
                    SourceType = ImageSource.Url,
                    FitMode = WallpaperFitMode.Fill,
                    EnableNotifications = false
                };
                _configService.SaveConfiguration(settings);

                // Mark first run complete
                _stateService.MarkFirstRunComplete();

                MessageBox.Show(
                    "Setup complete! Wallpaper Sync is now running.\n\n" +
                    "Left-click the tray icon to change settings.",
                    "Welcome to Wallpaper Sync",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CloseWindow?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? CloseWindow { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
