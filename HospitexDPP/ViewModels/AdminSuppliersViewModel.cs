using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public enum SupplierDrawerMode { None, New, Edit, ApiKey }

    public class AdminSuppliersViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<SupplierDetail> _allSuppliers = new();
        private List<RelationEntry> _allRelations = new();
        private string _searchText = string.Empty;
        private List<StatusFilterOption> _statusFilterOptions = StatusFilterOption.All;

        private SupplierDetail? _selectedSupplier;
        private SupplierDrawerMode _drawerMode = SupplierDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editSupplierId;
        private string _editSupplierName = string.Empty;
        private string _editSupplierLocation = string.Empty;
        private string _editFacilityRegistry = string.Empty;
        private string _editFacilityIdentifier = string.Empty;
        private string _editOperatorRegistry = string.Empty;
        private string _editOperatorIdentifier = string.Empty;
        private string _editCountryConfection = string.Empty;
        private string _editCountryDyeing = string.Empty;
        private string _editCountryWeaving = string.Empty;
        private string _editLei = string.Empty;
        private string _editGs1 = string.Empty;
        private bool _editIsActive = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after suppliers are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public AdminSuppliersViewModel()
        {
            _apiClient = App.ApiClient;

            AddCommand = new RelayCommand(_ => OpenNewDrawer());
            EditCommand = new RelayCommand(p => OpenEditDrawer(p as SupplierDetail));
            DeleteCommand = new RelayCommand(async p => await DeleteSupplierAsync(p as SupplierDetail));
            ToggleActiveCommand = new RelayCommand(async p => await ToggleActiveAsync(p as SupplierDetail));
            ShowApiKeyCommand = new RelayCommand(p => OpenApiKeyDrawer(p as SupplierDetail));
            RegenerateKeyCommand = new RelayCommand(async _ => await RegenerateKeyAsync(_selectedSupplier));

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(EditSupplierName) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());

            var saved = SettingsService.LoadFilter("admin_suppliers");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadSuppliersAsync();
        }

        public ObservableCollection<SupplierDetail> Suppliers { get; } = new();
        public List<StatusFilterOption> StatusFilterOptions
        {
            get => _statusFilterOptions;
            private set { _statusFilterOptions = value; OnPropertyChanged(); }
        }

        private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StatusFilterOption.IsSelected))
            {
                ApplyFilter();
                var sel = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value);
                SettingsService.SaveFilter("admin_suppliers", string.Join(",", sel));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public SupplierDetail? SelectedSupplier
        {
            get => _selectedSupplier;
            set { _selectedSupplier = value; OnPropertyChanged(); }
        }

        public bool IsDrawerOpen => _drawerMode != SupplierDrawerMode.None;
        public bool IsEditDrawer => _drawerMode == SupplierDrawerMode.New || _drawerMode == SupplierDrawerMode.Edit;
        public bool IsApiKeyDrawer => _drawerMode == SupplierDrawerMode.ApiKey;
        public bool ShowIsActive => _drawerMode == SupplierDrawerMode.Edit;
        public bool HasSuppliers => Suppliers.Count > 0;
        public int TotalCount => _allSuppliers.Count;

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                SupplierDrawerMode.New => Application.Current.TryFindResource("Drawer_NewSupplier") as string ?? "Ny leverantör",
                SupplierDrawerMode.Edit => Application.Current.TryFindResource("Drawer_EditSupplier") as string ?? "Redigera leverantör",
                SupplierDrawerMode.ApiKey => Application.Current.TryFindResource("Drawer_ApiKey") as string ?? "API-nyckel",
                _ => string.Empty
            };
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

        // Edit fields
        public string EditSupplierName
        {
            get => _editSupplierName;
            set { _editSupplierName = value; OnPropertyChanged(); }
        }

        public string EditSupplierLocation
        {
            get => _editSupplierLocation;
            set { _editSupplierLocation = value; OnPropertyChanged(); }
        }

        public string EditFacilityRegistry
        {
            get => _editFacilityRegistry;
            set { _editFacilityRegistry = value; OnPropertyChanged(); }
        }

        public string EditFacilityIdentifier
        {
            get => _editFacilityIdentifier;
            set { _editFacilityIdentifier = value; OnPropertyChanged(); }
        }

        public string EditOperatorRegistry
        {
            get => _editOperatorRegistry;
            set { _editOperatorRegistry = value; OnPropertyChanged(); }
        }

        public string EditOperatorIdentifier
        {
            get => _editOperatorIdentifier;
            set { _editOperatorIdentifier = value; OnPropertyChanged(); }
        }

        public string EditCountryConfection
        {
            get => _editCountryConfection;
            set { _editCountryConfection = value; OnPropertyChanged(); }
        }

        public string EditCountryDyeing
        {
            get => _editCountryDyeing;
            set { _editCountryDyeing = value; OnPropertyChanged(); }
        }

        public string EditCountryWeaving
        {
            get => _editCountryWeaving;
            set { _editCountryWeaving = value; OnPropertyChanged(); }
        }

        public string EditLei
        {
            get => _editLei;
            set { _editLei = value; OnPropertyChanged(); }
        }

        public string EditGs1
        {
            get => _editGs1;
            set { _editGs1 = value; OnPropertyChanged(); }
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set { _editIsActive = value; OnPropertyChanged(); }
        }

        public string? DrawerApiKey => _selectedSupplier?.ApiKey;

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ShowApiKeyCommand { get; }
        public ICommand RegenerateKeyCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }

        private void SetDrawerMode(SupplierDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsEditDrawer));
            OnPropertyChanged(nameof(IsApiKeyDrawer));
            OnPropertyChanged(nameof(ShowIsActive));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private void OpenNewDrawer()
        {
            _editSupplierId = null;
            EditSupplierName = string.Empty;
            EditSupplierLocation = string.Empty;
            EditFacilityRegistry = string.Empty;
            EditFacilityIdentifier = string.Empty;
            EditOperatorRegistry = string.Empty;
            EditOperatorIdentifier = string.Empty;
            EditCountryConfection = string.Empty;
            EditCountryDyeing = string.Empty;
            EditCountryWeaving = string.Empty;
            EditLei = string.Empty;
            EditGs1 = string.Empty;
            EditIsActive = true;
            StatusMessage = string.Empty;
            SetDrawerMode(SupplierDrawerMode.New);
        }

        private void OpenEditDrawer(SupplierDetail? supplier)
        {
            if (supplier == null) return;
            SelectedSupplier = supplier;
            _editSupplierId = supplier.Id;
            EditSupplierName = supplier.SupplierName ?? string.Empty;
            EditSupplierLocation = supplier.SupplierLocation ?? string.Empty;
            EditFacilityRegistry = supplier.FacilityRegistry ?? string.Empty;
            EditFacilityIdentifier = supplier.FacilityIdentifier ?? string.Empty;
            EditOperatorRegistry = supplier.OperatorRegistry ?? string.Empty;
            EditOperatorIdentifier = supplier.OperatorIdentifier ?? string.Empty;
            EditCountryConfection = supplier.CountryOfOriginConfection ?? string.Empty;
            EditCountryDyeing = supplier.CountryOfOriginDyeing ?? string.Empty;
            EditCountryWeaving = supplier.CountryOfOriginWeaving ?? string.Empty;
            EditLei = supplier.Lei ?? string.Empty;
            EditGs1 = supplier.Gs1CompanyPrefix ?? string.Empty;
            EditIsActive = supplier.IsActive == 1;
            StatusMessage = string.Empty;
            SetDrawerMode(SupplierDrawerMode.Edit);
        }

        private void OpenApiKeyDrawer(SupplierDetail? supplier)
        {
            if (supplier == null) return;
            SelectedSupplier = supplier;
            OnPropertyChanged(nameof(DrawerApiKey));
            SetDrawerMode(SupplierDrawerMode.ApiKey);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(SupplierDrawerMode.None);
        }

        private async Task SaveAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["supplier_name"] = EditSupplierName.Trim(),
                    ["supplier_location"] = NullIfEmpty(EditSupplierLocation),
                    ["facility_registry"] = NullIfEmpty(EditFacilityRegistry),
                    ["facility_identifier"] = NullIfEmpty(EditFacilityIdentifier),
                    ["operator_registry"] = NullIfEmpty(EditOperatorRegistry),
                    ["operator_identifier"] = NullIfEmpty(EditOperatorIdentifier),
                    ["country_of_origin_confection"] = NullIfEmpty(EditCountryConfection),
                    ["country_of_origin_dyeing"] = NullIfEmpty(EditCountryDyeing),
                    ["country_of_origin_weaving"] = NullIfEmpty(EditCountryWeaving),
                    ["lei"] = NullIfEmpty(EditLei),
                    ["gs1_company_prefix"] = NullIfEmpty(EditGs1),
                };

                payload["_is_active"] = EditIsActive ? 1 : 0;

                string? result;
                if (_drawerMode == SupplierDrawerMode.New)
                {
                    result = await _apiClient.PostAsync("/api/admin/suppliers", payload);
                }
                else
                {
                    result = await _apiClient.PutAsync($"/api/admin/suppliers/{_editSupplierId}", payload);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadSuppliersAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusMessage = $"Fel: {err.GetString()}";
                        return;
                    }
                }
                StatusMessage = Application.Current.TryFindResource("Msg_NoResponse") as string ?? "Inget svar från servern";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task DeleteSupplierAsync(SupplierDetail? supplier)
        {
            if (supplier == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{supplier.SupplierName}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteAsync($"/api/admin/suppliers/{supplier.Id}");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadSuppliersAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        MessageBox.Show($"Fel: {err.GetString()}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ToggleActiveAsync(SupplierDetail? supplier)
        {
            if (supplier == null) return;

            var newActive = supplier.IsActive == 1 ? 0 : 1;
            var payload = new Dictionary<string, object?>
            {
                ["supplier_name"] = supplier.SupplierName,
                ["_is_active"] = newActive
            };

            try
            {
                var json = await _apiClient.PutAsync($"/api/admin/suppliers/{supplier.Id}", payload);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadSuppliersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminSuppliers] ToggleActive error: {ex.Message}");
            }
        }

        private async Task RegenerateKeyAsync(SupplierDetail? supplier)
        {
            if (supplier == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_RegenerateKey") as string
                ?? "Nuvarande nyckel slutar fungera omedelbart. Fortsätt?";
            var result = MessageBox.Show(
                confirmText,
                Application.Current.TryFindResource("Action_RegenerateKey") as string ?? "Generera ny nyckel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.PostAsync($"/api/admin/suppliers/{supplier.Id}/regenerate-key", new { });
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        string? newKey = null;
                        if (data.TryGetProperty("api_key", out var k1)) newKey = k1.GetString();
                        else if (data.TryGetProperty("apiKey", out var k2)) newKey = k2.GetString();

                        if (newKey != null)
                        {
                            supplier.ApiKey = newKey;
                            OnPropertyChanged(nameof(DrawerApiKey));
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fel: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLanguageChanged()
        {
            var selectedValues = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _statusFilterOptions = StatusFilterOption.All;
            foreach (var opt in _statusFilterOptions)
            {
                opt.IsSelected = selectedValues.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(StatusFilterOptions));
            ApplyFilter();
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var json = await _apiClient.GetRawAsync("/api/admin/suppliers");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<SupplierDetail>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allSuppliers = items;
                    }
                }

                var relJson = await _apiClient.GetRawAsync("/api/admin/relations");
                if (relJson != null)
                {
                    using var doc = JsonDocument.Parse(relJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<RelationEntry>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allRelations = items;
                    }
                }

                // Compute brand counts per supplier
                var counts = _allRelations.GroupBy(r => r.SupplierId).ToDictionary(g => g.Key, g => g.Count());
                foreach (var supplier in _allSuppliers)
                    supplier.BrandCount = counts.TryGetValue(supplier.Id, out var c) ? c : 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminSuppliers] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadSuppliersAsync()
        {
            _allSuppliers.Clear();
            Suppliers.Clear();
            await LoadSuppliersAsync();
        }

        private void ApplyFilter()
        {
            Suppliers.Clear();
            var filter = _searchText.Trim();
            IEnumerable<SupplierDetail> filtered = _allSuppliers;

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(s => s.SupplierName != null &&
                    s.SupplierName.Contains(filter, StringComparison.OrdinalIgnoreCase));

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(s =>
                    (activeFilters.Contains("active") && s.IsActive == 1) ||
                    (activeFilters.Contains("inactive") && s.IsActive != 1));

            foreach (var s in filtered)
                Suppliers.Add(s);

            OnPropertyChanged(nameof(HasSuppliers));
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
