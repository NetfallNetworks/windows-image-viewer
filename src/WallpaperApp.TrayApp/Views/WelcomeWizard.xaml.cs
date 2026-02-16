using System.Windows;
using WallpaperApp.Configuration;
using WallpaperApp.Services;
using WallpaperApp.TrayApp.ViewModels;

namespace WallpaperApp.TrayApp.Views
{
    /// <summary>
    /// Interaction logic for WelcomeWizard.xaml
    /// </summary>
    public partial class WelcomeWizard : Window
    {
        public WelcomeWizard(IConfigurationService configService, IAppStateService stateService)
        {
            InitializeComponent();

            var viewModel = new WelcomeViewModel(configService, stateService);
            viewModel.CloseWindow = () => Close();

            DataContext = viewModel;
        }
    }
}
