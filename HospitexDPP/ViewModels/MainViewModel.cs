using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Resources;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView = null!;
        private BrandViewModel? _brandVm;
        private SupplierViewModel? _supplierVm;
        private bool _isDualRole;
        private bool _isBrandActive;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(_ => NavigateToLogin(), _ => !App.ApiClient.IsAuthenticated);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => App.ApiClient.IsAuthenticated);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
            AboutCommand = new RelayCommand(_ => MessageBox.Show($"Hospitex DPP v{App.Version}\nDigital Product Passport för textil", "Om Hospitex DPP"));
            SwitchToBrandCommand = new RelayCommand(_ => IsBrandActive = true);
            SwitchToSupplierCommand = new RelayCommand(_ => IsBrandActive = false);

            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public bool IsDualRole
        {
            get => _isDualRole;
            set { _isDualRole = value; OnPropertyChanged(); }
        }

        public bool IsBrandActive
        {
            get => _isBrandActive;
            set
            {
                if (_isBrandActive == value) return;
                _isBrandActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSupplierActive));
                if (_isDualRole)
                    CurrentView = _isBrandActive ? (object)_brandVm! : _supplierVm!;
            }
        }

        public bool IsSupplierActive => !IsBrandActive;

        public string BrandLabel => App.Session?.BrandName ?? "Brand";
        public string SupplierLabel => App.Session?.SupplierName ?? "Supplier";

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
        public ICommand SwitchToBrandCommand { get; }
        public ICommand SwitchToSupplierCommand { get; }

        private void OnLoginSuccess()
        {
            RefreshSessionProperties();

            _brandVm = null;
            _supplierVm = null;
            _isBrandActive = false;

            if (App.Session!.IsAdmin)
            {
                IsDualRole = false;
                CurrentView = new AdminViewModel();
                return;
            }

            if (App.Session.IsBrand)
                _brandVm = new BrandViewModel();
            if (App.Session.IsSupplier)
                _supplierVm = new SupplierViewModel();

            IsDualRole = _brandVm != null && _supplierVm != null;

            if (IsDualRole)
            {
                // Setter also sets CurrentView
                IsBrandActive = true;
            }
            else
            {
                CurrentView = (object?)_brandVm ?? _supplierVm!;
            }
        }

        private void Disconnect()
        {
            App.ApiClient.Logout();
            App.Session = null;
            _brandVm = null;
            _supplierVm = null;
            IsDualRole = false;

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
            OnPropertyChanged(nameof(BrandLabel));
            OnPropertyChanged(nameof(SupplierLabel));
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
