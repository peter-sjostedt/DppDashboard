using System.Windows;
using System.Windows.Controls;
using HospitexDPP.Helpers;
using HospitexDPP.Resources;
using HospitexDPP.Services;
using HospitexDPP.ViewModels;

namespace HospitexDPP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowState();
            LanguageService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // {h:Loc} bindings update via TranslationSource.Refresh(),
            // StatusBadge updates via its own LanguageChanged subscription,
            // and ViewModels fire PropertyChanged for converter-based bindings.
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            var menu = new ContextMenu();

            // Profile section (show connected role info)
            if (App.Session != null)
            {
                if (App.Session.IsBrand)
                {
                    var brandLabel = Strings.ResourceManager.GetString("Role_Brand", Strings.Culture) ?? "Brand";
                    menu.Items.Add(new MenuItem
                    {
                        Header = $"{brandLabel}: {App.Session.BrandName}",
                        IsEnabled = false
                    });
                }
                if (App.Session.IsSupplier)
                {
                    var supplierLabel = Strings.ResourceManager.GetString("Role_Supplier", Strings.Culture) ?? "Supplier";
                    menu.Items.Add(new MenuItem
                    {
                        Header = $"{supplierLabel}: {App.Session.SupplierName}",
                        IsEnabled = false
                    });
                }
                if (App.Session.IsAdmin)
                {
                    menu.Items.Add(new MenuItem { Header = "Admin", IsEnabled = false });
                }
                menu.Items.Add(new Separator());
            }

            // Language submenu
            var langHeader = new MenuItem { Header = Strings.ResourceManager.GetString("Menu_Settings", Strings.Culture) ?? "Settings" };
            var svItem = new MenuItem { Header = Strings.ResourceManager.GetString("Settings_Swedish", Strings.Culture) ?? "Svenska" };
            svItem.Click += (_, _) => LanguageService.SetLanguage("sv");
            var enItem = new MenuItem { Header = Strings.ResourceManager.GetString("Settings_English", Strings.Culture) ?? "English" };
            enItem.Click += (_, _) => LanguageService.SetLanguage("en");
            langHeader.Items.Add(svItem);
            langHeader.Items.Add(enItem);
            menu.Items.Add(langHeader);

            // About
            var aboutItem = new MenuItem { Header = Strings.ResourceManager.GetString("Menu_About", Strings.Culture) ?? "About" };
            aboutItem.Click += (_, _) => vm.AboutCommand.Execute(null);
            menu.Items.Add(aboutItem);

            menu.Items.Add(new Separator());

            // Disconnect/Logout
            var logoutItem = new MenuItem
            {
                Header = Strings.ResourceManager.GetString("Menu_Logout", Strings.Culture) ?? "Log out",
                IsEnabled = vm.DisconnectCommand.CanExecute(null)
            };
            logoutItem.Click += (_, _) => vm.DisconnectCommand.Execute(null);
            menu.Items.Add(logoutItem);

            menu.PlacementTarget = btn;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
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
