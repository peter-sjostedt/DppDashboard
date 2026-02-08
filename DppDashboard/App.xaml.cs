using System.Windows;
using DppDashboard.Services;
using DppDashboard.ViewModels;

namespace DppDashboard
{
    public partial class App : Application
    {
        public static ApiClient ApiClient { get; } = new ApiClient();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            mainWindow.Show();
        }
    }
}
