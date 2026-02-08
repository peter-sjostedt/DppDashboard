using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace DppDashboard.ViewModels
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
            ShowBrandsCommand = new RelayCommand(_ => ShowBrands(), _ => App.ApiClient.IsAuthenticated);
            ShowSuppliersCommand = new RelayCommand(_ => ShowSuppliers(), _ => App.ApiClient.IsAuthenticated);
            ShowRelationsCommand = new RelayCommand(_ => ShowRelations(), _ => App.ApiClient.IsAuthenticated);

            AboutCommand = new RelayCommand(_ => MessageBox.Show("DPP Dashboard v1.0\nDigital Product Passport för textil", "Om DPP Dashboard"));

            CurrentView = new LoginViewModel(NavigateToDashboard);
        }

        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand ExitCommand { get; }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowBrandsCommand { get; }
        public ICommand ShowSuppliersCommand { get; }
        public ICommand ShowRelationsCommand { get; }

        public ICommand AboutCommand { get; }

        private void NavigateToDashboard()
        {
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
            CurrentView = new LoginViewModel(NavigateToDashboard);
        }

        private void NavigateToLogin()
        {
            CurrentView = new LoginViewModel(NavigateToDashboard);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
