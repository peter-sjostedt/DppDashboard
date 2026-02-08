using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DppDashboard.Services;

namespace DppDashboard.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        private readonly Action _onLoginSuccess;
        private string _adminKey = "dpp_admin_master_key_2024_secure";
        private string _statusMessage = string.Empty;
        private bool _isLoggingIn;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LoginViewModel(Action onLoginSuccess)
        {
            _apiClient = App.ApiClient;
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new RelayCommand(async _ => await DoLoginAsync(), _ => !IsLoggingIn);
        }

        public string AdminKey
        {
            get => _adminKey;
            set { _adminKey = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        private async Task DoLoginAsync()
        {
            IsLoggingIn = true;
            StatusMessage = "Ansluter...";

            try
            {
                var success = await _apiClient.LoginAsync(AdminKey);

                if (success)
                {
                    _onLoginSuccess();
                }
                else
                {
                    StatusMessage = "Kunde inte ansluta: kontrollera nyckel och nätverksåtkomst";
                }
            }
            catch
            {
                StatusMessage = "Kunde inte ansluta: kontrollera nyckel och nätverksåtkomst";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}
