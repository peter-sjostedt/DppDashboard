using System.Windows;
using HospitexDPP.Services;
using HospitexDPP.ViewModels;

namespace HospitexDPP
{
    public partial class App : Application
    {
        public static ApiClient ApiClient { get; } = new ApiClient();
        public static SessionContext? Session { get; set; }

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
