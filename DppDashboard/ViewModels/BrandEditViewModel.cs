using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using DppDashboard.Models;

namespace DppDashboard.ViewModels
{
    public class BrandEditViewModel : INotifyPropertyChanged
    {
        private readonly int? _brandId;
        private string _brandName = string.Empty;
        private string _logoUrl = string.Empty;
        private string _subBrand = string.Empty;
        private string _parentCompany = string.Empty;
        private string _trader = string.Empty;
        private string _traderLocation = string.Empty;
        private string _lei = string.Empty;
        private string _gs1CompanyPrefix = string.Empty;
        private bool _isActive = true;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<bool>? RequestClose;

        public BrandEditViewModel(BrandSummary? brand)
        {
            _brandId = brand?.Id;
            IsNew = brand == null;
            DialogTitle = brand == null ? "Ny brand" : $"Redigera: {brand.BrandName}";

            if (brand != null)
            {
                _brandName = brand.BrandName ?? string.Empty;
                _logoUrl = brand.LogoUrl ?? string.Empty;
                _subBrand = brand.SubBrand ?? string.Empty;
                _parentCompany = brand.ParentCompany ?? string.Empty;
                _trader = brand.Trader ?? string.Empty;
                _traderLocation = brand.TraderLocation ?? string.Empty;
                _lei = brand.Lei ?? string.Empty;
                _gs1CompanyPrefix = brand.Gs1CompanyPrefix ?? string.Empty;
                _isActive = brand.IsActive == 1;
            }

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(BrandName) && !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        public bool IsNew { get; }
        public string DialogTitle { get; }
        public bool ShowIsActive => !IsNew;

        public string BrandName
        {
            get => _brandName;
            set { _brandName = value; OnPropertyChanged(); }
        }

        public string LogoUrl
        {
            get => _logoUrl;
            set { _logoUrl = value; OnPropertyChanged(); }
        }

        public string SubBrand
        {
            get => _subBrand;
            set { _subBrand = value; OnPropertyChanged(); }
        }

        public string ParentCompany
        {
            get => _parentCompany;
            set { _parentCompany = value; OnPropertyChanged(); }
        }

        public string Trader
        {
            get => _trader;
            set { _trader = value; OnPropertyChanged(); }
        }

        public string TraderLocation
        {
            get => _traderLocation;
            set { _traderLocation = value; OnPropertyChanged(); }
        }

        public string Lei
        {
            get => _lei;
            set { _lei = value; OnPropertyChanged(); }
        }

        public string Gs1CompanyPrefix
        {
            get => _gs1CompanyPrefix;
            set { _gs1CompanyPrefix = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set { _isSaving = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task SaveAsync()
        {
            IsSaving = true;
            StatusMessage = "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["brand_name"] = BrandName.Trim(),
                    ["logo_url"] = string.IsNullOrWhiteSpace(LogoUrl) ? null : LogoUrl.Trim(),
                    ["sub_brand"] = string.IsNullOrWhiteSpace(SubBrand) ? null : SubBrand.Trim(),
                    ["parent_company"] = string.IsNullOrWhiteSpace(ParentCompany) ? null : ParentCompany.Trim(),
                    ["trader"] = string.IsNullOrWhiteSpace(Trader) ? null : Trader.Trim(),
                    ["trader_location"] = string.IsNullOrWhiteSpace(TraderLocation) ? null : TraderLocation.Trim(),
                    ["lei"] = string.IsNullOrWhiteSpace(Lei) ? null : Lei.Trim(),
                    ["gs1_company_prefix"] = string.IsNullOrWhiteSpace(Gs1CompanyPrefix) ? null : Gs1CompanyPrefix.Trim(),
                };

                if (!IsNew)
                    payload["_is_active"] = IsActive ? 1 : 0;

                string? result;
                if (IsNew)
                {
                    Debug.WriteLine($"[BrandEdit] POST /api/admin/brands => {JsonSerializer.Serialize(payload)}");
                    result = await App.ApiClient.PostAsync("/api/admin/brands", payload);
                }
                else
                {
                    Debug.WriteLine($"[BrandEdit] PUT /api/admin/brands/{_brandId} => {JsonSerializer.Serialize(payload)}");
                    result = await App.ApiClient.PutAsync($"/api/admin/brands/{_brandId}", payload);
                }

                Debug.WriteLine($"[BrandEdit] Response: {result}");

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        RequestClose?.Invoke(true);
                        return;
                    }

                    if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        StatusMessage = $"Fel: {errorProp.GetString()}";
                        return;
                    }
                }

                StatusMessage = "Fel: Inget svar från servern";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                Debug.WriteLine($"[BrandEdit] ERROR: {ex}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
