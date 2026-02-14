using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView = null!;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(_ => NavigateToLogin(), _ => !App.ApiClient.IsAuthenticated);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => App.ApiClient.IsAuthenticated);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            AboutCommand = new RelayCommand(_ => MessageBox.Show("Hospitex DPP v1.0\nDigital Product Passport för textil", "Om Hospitex DPP"));

            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public string SessionInfo
        {
            get
            {
                if (App.Session == null) return string.Empty;
                var parts = new List<string>();
                if (App.Session.IsAdmin) parts.Add("Admin");
                if (App.Session.IsBrand) parts.Add($"Brand: {App.Session.BrandName}");
                if (App.Session.IsSupplier) parts.Add($"Supplier: {App.Session.SupplierName}");
                return string.Join(" | ", parts);
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }

        private void OnLoginSuccess()
        {
            RefreshSessionProperties();
            if (App.Session!.IsAdmin)
                CurrentView = new AdminViewModel();
            else if (App.Session.IsBrand)
                CurrentView = new BrandViewModel();
            else if (App.Session.IsSupplier)
                CurrentView = new SupplierViewModel();
        }

        private void Disconnect()
        {
            App.ApiClient.Logout();
            App.Session = null;

            // Clear saved keys but preserve language preference
            var settings = SettingsService.Load();
            settings.Keys.Clear();
            SettingsService.Save(settings);

            RefreshSessionProperties();
            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        private void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        private void RefreshSessionProperties()
        {
            OnPropertyChanged(nameof(SessionInfo));
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
