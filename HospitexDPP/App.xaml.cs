using System.Threading.Tasks;
using System.Windows;
using HospitexDPP.Services;
using HospitexDPP.ViewModels;

namespace HospitexDPP
{
    public partial class App : Application
    {
        public const string Version = "0.8.70";

        public static ApiClient ApiClient { get; } = new ApiClient();
        public static SessionContext? Session { get; set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LanguageService.Initialize();

            var splash = new SplashWindow();
            splash.Show();

            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            await Task.Delay(2000);

            splash.Close();
            mainWindow.Show();
        }
    }
}
