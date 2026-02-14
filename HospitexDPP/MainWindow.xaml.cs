using System.Windows;
using HospitexDPP.Services;

namespace HospitexDPP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowState();
        }

        private void RestoreWindowState()
        {
            var settings = SettingsService.Load();
            var w = settings.Window;
            if (w == null) return;

            Width = Math.Max(w.Width, MinWidth);
            Height = Math.Max(w.Height, MinHeight);

            if (!double.IsNaN(w.Left) && !double.IsNaN(w.Top))
            {
                // Check if saved position is at least partially visible on the virtual screen
                var visibleLeft = w.Left + Width > SystemParameters.VirtualScreenLeft
                    && w.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
                var visibleTop = w.Top + Height > SystemParameters.VirtualScreenTop
                    && w.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

                if (visibleLeft && visibleTop)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Left = w.Left;
                    Top = w.Top;
                }
            }

            if (w.Maximized)
                WindowState = WindowState.Maximized;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var settings = SettingsService.Load();

            var isMaximized = WindowState == WindowState.Maximized;
            settings.Window = new WindowSettings
            {
                Width = isMaximized ? RestoreBounds.Width : Width,
                Height = isMaximized ? RestoreBounds.Height : Height,
                Left = isMaximized ? RestoreBounds.Left : Left,
                Top = isMaximized ? RestoreBounds.Top : Top,
                Maximized = isMaximized
            };

            SettingsService.Save(settings);
        }
    }
}
