using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using DppDashboard.Models;

namespace DppDashboard.ViewModels
{
    public class SupplierEditViewModel : INotifyPropertyChanged
    {
        private readonly int? _supplierId;
        private string _supplierName = string.Empty;
        private string _supplierLocation = string.Empty;
        private string _facilityRegistry = string.Empty;
        private string _facilityIdentifier = string.Empty;
        private string _operatorRegistry = string.Empty;
        private string _operatorIdentifier = string.Empty;
        private string _countryOfOriginConfection = string.Empty;
        private string _countryOfOriginDyeing = string.Empty;
        private string _countryOfOriginWeaving = string.Empty;
        private string _lei = string.Empty;
        private string _gs1CompanyPrefix = string.Empty;
        private bool _isActive = true;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<bool>? RequestClose;

        public SupplierEditViewModel(SupplierDetail? supplier)
        {
            _supplierId = supplier?.Id;
            IsNew = supplier == null;
            DialogTitle = supplier == null ? "Ny supplier" : $"Redigera: {supplier.SupplierName}";

            if (supplier != null)
            {
                _supplierName = supplier.SupplierName ?? string.Empty;
                _supplierLocation = supplier.SupplierLocation ?? string.Empty;
                _facilityRegistry = supplier.FacilityRegistry ?? string.Empty;
                _facilityIdentifier = supplier.FacilityIdentifier ?? string.Empty;
                _operatorRegistry = supplier.OperatorRegistry ?? string.Empty;
                _operatorIdentifier = supplier.OperatorIdentifier ?? string.Empty;
                _countryOfOriginConfection = supplier.CountryOfOriginConfection ?? string.Empty;
                _countryOfOriginDyeing = supplier.CountryOfOriginDyeing ?? string.Empty;
                _countryOfOriginWeaving = supplier.CountryOfOriginWeaving ?? string.Empty;
                _lei = supplier.Lei ?? string.Empty;
                _gs1CompanyPrefix = supplier.Gs1CompanyPrefix ?? string.Empty;
                _isActive = supplier.IsActive == 1;
            }

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(SupplierName) && !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        public bool IsNew { get; }
        public string DialogTitle { get; }
        public bool ShowIsActive => !IsNew;

        public string SupplierName
        {
            get => _supplierName;
            set { _supplierName = value; OnPropertyChanged(); }
        }

        public string SupplierLocation
        {
            get => _supplierLocation;
            set { _supplierLocation = value; OnPropertyChanged(); }
        }

        public string FacilityRegistry
        {
            get => _facilityRegistry;
            set { _facilityRegistry = value; OnPropertyChanged(); }
        }

        public string FacilityIdentifier
        {
            get => _facilityIdentifier;
            set { _facilityIdentifier = value; OnPropertyChanged(); }
        }

        public string OperatorRegistry
        {
            get => _operatorRegistry;
            set { _operatorRegistry = value; OnPropertyChanged(); }
        }

        public string OperatorIdentifier
        {
            get => _operatorIdentifier;
            set { _operatorIdentifier = value; OnPropertyChanged(); }
        }

        public string CountryOfOriginConfection
        {
            get => _countryOfOriginConfection;
            set { _countryOfOriginConfection = value; OnPropertyChanged(); }
        }

        public string CountryOfOriginDyeing
        {
            get => _countryOfOriginDyeing;
            set { _countryOfOriginDyeing = value; OnPropertyChanged(); }
        }

        public string CountryOfOriginWeaving
        {
            get => _countryOfOriginWeaving;
            set { _countryOfOriginWeaving = value; OnPropertyChanged(); }
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
                    ["supplier_name"] = SupplierName.Trim(),
                    ["supplier_location"] = string.IsNullOrWhiteSpace(SupplierLocation) ? null : SupplierLocation.Trim(),
                    ["facility_registry"] = string.IsNullOrWhiteSpace(FacilityRegistry) ? null : FacilityRegistry.Trim(),
                    ["facility_identifier"] = string.IsNullOrWhiteSpace(FacilityIdentifier) ? null : FacilityIdentifier.Trim(),
                    ["operator_registry"] = string.IsNullOrWhiteSpace(OperatorRegistry) ? null : OperatorRegistry.Trim(),
                    ["operator_identifier"] = string.IsNullOrWhiteSpace(OperatorIdentifier) ? null : OperatorIdentifier.Trim(),
                    ["country_of_origin_confection"] = string.IsNullOrWhiteSpace(CountryOfOriginConfection) ? null : CountryOfOriginConfection.Trim().ToUpperInvariant(),
                    ["country_of_origin_dyeing"] = string.IsNullOrWhiteSpace(CountryOfOriginDyeing) ? null : CountryOfOriginDyeing.Trim().ToUpperInvariant(),
                    ["country_of_origin_weaving"] = string.IsNullOrWhiteSpace(CountryOfOriginWeaving) ? null : CountryOfOriginWeaving.Trim().ToUpperInvariant(),
                    ["lei"] = string.IsNullOrWhiteSpace(Lei) ? null : Lei.Trim(),
                    ["gs1_company_prefix"] = string.IsNullOrWhiteSpace(Gs1CompanyPrefix) ? null : Gs1CompanyPrefix.Trim(),
                };

                if (!IsNew)
                    payload["_is_active"] = IsActive ? 1 : 0;

                string? result;
                if (IsNew)
                {
                    Debug.WriteLine($"[SupplierEdit] POST /api/admin/suppliers => {JsonSerializer.Serialize(payload)}");
                    result = await App.ApiClient.PostAsync("/api/admin/suppliers", payload);
                }
                else
                {
                    Debug.WriteLine($"[SupplierEdit] PUT /api/admin/suppliers/{_supplierId} => {JsonSerializer.Serialize(payload)}");
                    result = await App.ApiClient.PutAsync($"/api/admin/suppliers/{_supplierId}", payload);
                }

                Debug.WriteLine($"[SupplierEdit] Response: {result}");

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
                Debug.WriteLine($"[SupplierEdit] ERROR: {ex}");
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
