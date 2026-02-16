using System.Windows;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using WallpaperApp.TrayApp.ViewModels;

namespace WallpaperApp.TrayApp.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(
            IConfigurationService configService,
            IAppStateService stateService,
            IWallpaperService wallpaperService,
            IImageValidator imageValidator)
        {
            InitializeComponent();

            _viewModel = new SettingsViewModel(
                configService,
                stateService,
                wallpaperService,
                imageValidator);

            _viewModel.CloseWindow = () => Close();

            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
