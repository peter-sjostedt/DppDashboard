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

            ShowDashboardCommand = new RelayCommand(_ => ShowDashboard(), _ => App.ApiClient.IsAuthenticated);
            ShowBrandsCommand = new RelayCommand(_ => ShowBrands(), _ => App.ApiClient.IsAuthenticated && CanSeeBrands);
            ShowSuppliersCommand = new RelayCommand(_ => ShowSuppliers(), _ => App.ApiClient.IsAuthenticated && CanSeeSuppliers);
            ShowRelationsCommand = new RelayCommand(_ => ShowRelations(), _ => App.ApiClient.IsAuthenticated && CanSeeRelations);

            AboutCommand = new RelayCommand(_ => MessageBox.Show("DPP Dashboard v1.0\nDigital Product Passport för textil", "Om DPP Dashboard"));

            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        // Role-based visibility properties for menu binding
        public bool CanSeeBrands => App.Session != null && (App.Session.IsAdmin || App.Session.IsBrand);
        public bool CanSeeSuppliers => App.Session != null && (App.Session.IsAdmin || App.Session.IsSupplier);
        public bool CanSeeRelations => App.Session != null && App.Session.IsAdmin;

        // Status bar info
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

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowBrandsCommand { get; }
        public ICommand ShowSuppliersCommand { get; }
        public ICommand ShowRelationsCommand { get; }

        public ICommand AboutCommand { get; }

        private void OnLoginSuccess()
        {
            RefreshSessionProperties();
            ShowDashboard();
        }

        private void ShowDashboard()
        {
            CurrentView = new DashboardHomeViewModel();
        }

        private void ShowBrands()
        {
            CurrentView = new BrandsViewModel();
        }

        private void ShowSuppliers()
        {
            CurrentView = new SuppliersViewModel();
        }

        private void ShowRelations()
        {
            CurrentView = new RelationsViewModel();
        }

        private void Disconnect()
        {
            App.ApiClient.Logout();
            App.Session = null;
            RefreshSessionProperties();
            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        private void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(OnLoginSuccess);
        }

        private void RefreshSessionProperties()
        {
            OnPropertyChanged(nameof(CanSeeBrands));
            OnPropertyChanged(nameof(CanSeeSuppliers));
            OnPropertyChanged(nameof(CanSeeRelations));
            OnPropertyChanged(nameof(SessionInfo));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
