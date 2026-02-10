using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;
        private readonly Action _onLoginSuccess;
        private string _brandKey = string.Empty;
        private string _supplierKey = string.Empty;
        private bool _rememberMe;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private bool _isLoggingIn;

        public event PropertyChangedEventHandler? PropertyChanged;

        public LoginViewModel(Action onLoginSuccess)
        {
            _apiClient = App.ApiClient;
            _onLoginSuccess = onLoginSuccess;
            LoginCommand = new RelayCommand(async _ => await DoLoginAsync(), _ => !IsLoggingIn && HasAnyKey);

            _ = TryAutoLoginAsync();
        }

        public string BrandKey
        {
            get => _brandKey;
            set { _brandKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAnyKey)); }
        }

        public string SupplierKey
        {
            get => _supplierKey;
            set { _supplierKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAnyKey)); }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set { _rememberMe = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool StatusIsError
        {
            get => _statusIsError;
            set { _statusIsError = value; OnPropertyChanged(); }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(); }
        }

        public bool HasAnyKey => !string.IsNullOrWhiteSpace(BrandKey) || !string.IsNullOrWhiteSpace(SupplierKey);

        public ICommand LoginCommand { get; }

        private async Task TryAutoLoginAsync()
        {
            var settings = SettingsService.Load();
            if (settings.Keys.Count == 0) return;

            IsLoggingIn = true;
            StatusIsError = false;
            StatusMessage = "Validerar sparade nycklar...";

            try
            {
                var session = new SessionContext();
                bool allValid = true;

                foreach (var saved in settings.Keys)
                {
                    var result = await _apiClient.TestKeyAsync(saved.Key);
                    if (result == null)
                    {
                        allValid = false;
                        break;
                    }

                    ApplyRoleToSession(session, result.Value.role, saved.Key, result.Value.name, result.Value.id);
                }

                if (allValid && (session.IsAdmin || session.IsBrand || session.IsSupplier))
                {
                    App.Session = session;
                    _apiClient.ConfigureSession(session.AdminKey);
                    Debug.WriteLine($"[Login] Auto-login OK: Admin={session.IsAdmin}, Brand={session.IsBrand} ({session.BrandName}), Supplier={session.IsSupplier} ({session.SupplierName})");
                    _onLoginSuccess();
                    return;
                }

                StatusIsError = true;
                StatusMessage = "Sparad nyckel är inte längre giltig";
                SettingsService.Clear();
            }
            catch (Exception ex)
            {
                StatusIsError = true;
                StatusMessage = $"Kunde inte validera sparade nycklar: {ex.Message}";
                Debug.WriteLine($"[Login] Auto-login error: {ex}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private async Task DoLoginAsync()
        {
            if (!HasAnyKey)
            {
                StatusIsError = true;
                StatusMessage = "Ange minst en nyckel";
                return;
            }

            IsLoggingIn = true;
            StatusIsError = false;
            StatusMessage = "Validerar nycklar...";

            try
            {
                var session = new SessionContext();
                var savedKeys = new List<SavedKey>();
                bool hasError = false;

                // Test brand field key
                if (!string.IsNullOrWhiteSpace(BrandKey))
                {
                    var result = await _apiClient.TestKeyAsync(BrandKey.Trim());
                    if (result == null)
                    {
                        StatusIsError = true;
                        StatusMessage = "Brand-nyckeln kunde inte valideras";
                        hasError = true;
                    }
                    else
                    {
                        ApplyRoleToSession(session, result.Value.role, BrandKey.Trim(), result.Value.name, result.Value.id);
                        savedKeys.Add(new SavedKey
                        {
                            Key = BrandKey.Trim(),
                            Role = result.Value.role,
                            Name = result.Value.name ?? result.Value.role
                        });
                        Debug.WriteLine($"[Login] Brand field: role={result.Value.role}, name={result.Value.name}, id={result.Value.id}");
                    }
                }

                // Test supplier field key
                if (!hasError && !string.IsNullOrWhiteSpace(SupplierKey))
                {
                    var result = await _apiClient.TestKeyAsync(SupplierKey.Trim());
                    if (result == null)
                    {
                        StatusIsError = true;
                        StatusMessage = "Supplier-nyckeln kunde inte valideras";
                        hasError = true;
                    }
                    else
                    {
                        ApplyRoleToSession(session, result.Value.role, SupplierKey.Trim(), result.Value.name, result.Value.id);
                        savedKeys.Add(new SavedKey
                        {
                            Key = SupplierKey.Trim(),
                            Role = result.Value.role,
                            Name = result.Value.name ?? result.Value.role
                        });
                        Debug.WriteLine($"[Login] Supplier field: role={result.Value.role}, name={result.Value.name}, id={result.Value.id}");
                    }
                }

                if (hasError)
                {
                    IsLoggingIn = false;
                    return;
                }

                if (!session.IsAdmin && !session.IsBrand && !session.IsSupplier)
                {
                    StatusIsError = true;
                    StatusMessage = "Ingen giltig roll hittades";
                    IsLoggingIn = false;
                    return;
                }

                // Save settings if remember me
                if (RememberMe)
                {
                    SettingsService.Save(new AppSettings { Keys = savedKeys });
                    Debug.WriteLine($"[Login] Saved {savedKeys.Count} key(s) to settings");
                }
                else
                {
                    SettingsService.Clear();
                }

                App.Session = session;
                _apiClient.ConfigureSession(session.AdminKey);
                Debug.WriteLine($"[Login] Login OK: Admin={session.IsAdmin}, Brand={session.IsBrand} ({session.BrandName}), Supplier={session.IsSupplier} ({session.SupplierName})");
                _onLoginSuccess();
            }
            catch (Exception ex)
            {
                StatusIsError = true;
                StatusMessage = $"Fel vid anslutning: {ex.Message}";
                Debug.WriteLine($"[Login] Login error: {ex}");
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private static void ApplyRoleToSession(SessionContext session, string role, string key, string? name, int? id)
        {
            switch (role)
            {
                case "admin":
                    session.AdminKey = key;
                    break;
                case "brand":
                    session.BrandKey = key;
                    session.BrandName = name;
                    session.BrandId = id;
                    break;
                case "supplier":
                    session.SupplierKey = key;
                    session.SupplierName = name;
                    session.SupplierId = id;
                    break;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
