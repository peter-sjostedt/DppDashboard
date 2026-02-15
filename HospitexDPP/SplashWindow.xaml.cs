using System.Windows;

namespace HospitexDPP
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            VersionText.Text = $"v{App.Version}";
        }
    }
}
