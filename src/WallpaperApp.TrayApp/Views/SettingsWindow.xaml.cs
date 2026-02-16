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
        public SettingsWindow(
            IConfigurationService configService,
            IAppStateService stateService,
            IWallpaperService wallpaperService,
            IImageValidator imageValidator)
        {
            InitializeComponent();

            var viewModel = new SettingsViewModel(
                configService,
                stateService,
                wallpaperService,
                imageValidator);

            viewModel.CloseWindow = () => Close();

            DataContext = viewModel;
        }
    }
}
