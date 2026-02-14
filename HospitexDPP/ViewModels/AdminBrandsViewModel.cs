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
    public enum BrandDrawerMode { None, New, Edit, ApiKey }

    public class AdminBrandsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<BrandSummary> _allBrands = new();
        private List<RelationEntry> _allRelations = new();
        private string _searchText = string.Empty;
        private List<StatusFilterOption> _statusFilterOptions = StatusFilterOption.All;

        private BrandSummary? _selectedBrand;
        private BrandDrawerMode _drawerMode = BrandDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editBrandId;
        private string _editBrandName = string.Empty;
        private string _editLogoUrl = string.Empty;
        private string _editSubBrand = string.Empty;
        private string _editParentCompany = string.Empty;
        private string _editTrader = string.Empty;
        private string _editTraderLocation = string.Empty;
        private string _editLei = string.Empty;
        private string _editGs1 = string.Empty;
        private bool _editIsActive = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after brands are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public AdminBrandsViewModel()
        {
            _apiClient = App.ApiClient;

            AddCommand = new RelayCommand(_ => OpenNewDrawer());
            EditCommand = new RelayCommand(p => OpenEditDrawer(p as BrandSummary));
            DeleteCommand = new RelayCommand(async p => await DeleteBrandAsync(p as BrandSummary));
            ToggleActiveCommand = new RelayCommand(async p => await ToggleActiveAsync(p as BrandSummary));
            ShowApiKeyCommand = new RelayCommand(p => OpenApiKeyDrawer(p as BrandSummary));
            RegenerateKeyCommand = new RelayCommand(async _ => await RegenerateKeyAsync(_selectedBrand));

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(EditBrandName) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());

            var saved = SettingsService.LoadFilter("admin_brands");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadBrandsAsync();
        }

        public ObservableCollection<BrandSummary> Brands { get; } = new();
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
                SettingsService.SaveFilter("admin_brands", string.Join(",", sel));
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

        public BrandSummary? SelectedBrand
        {
            get => _selectedBrand;
            set { _selectedBrand = value; OnPropertyChanged(); }
        }

        public bool IsDrawerOpen => _drawerMode != BrandDrawerMode.None;
        public bool IsEditDrawer => _drawerMode == BrandDrawerMode.New || _drawerMode == BrandDrawerMode.Edit;
        public bool IsApiKeyDrawer => _drawerMode == BrandDrawerMode.ApiKey;
        public bool ShowIsActive => _drawerMode == BrandDrawerMode.Edit;
        public bool HasBrands => Brands.Count > 0;
        public int TotalCount => _allBrands.Count;

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                BrandDrawerMode.New => Application.Current.TryFindResource("Drawer_NewBrand") as string ?? "Nytt varumärke",
                BrandDrawerMode.Edit => Application.Current.TryFindResource("Drawer_EditBrand") as string ?? "Redigera varumärke",
                BrandDrawerMode.ApiKey => Application.Current.TryFindResource("Drawer_ApiKey") as string ?? "API-nyckel",
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
        public string EditBrandName
        {
            get => _editBrandName;
            set { _editBrandName = value; OnPropertyChanged(); }
        }

        public string EditLogoUrl
        {
            get => _editLogoUrl;
            set { _editLogoUrl = value; OnPropertyChanged(); }
        }

        public string EditSubBrand
        {
            get => _editSubBrand;
            set { _editSubBrand = value; OnPropertyChanged(); }
        }

        public string EditParentCompany
        {
            get => _editParentCompany;
            set { _editParentCompany = value; OnPropertyChanged(); }
        }

        public string EditTrader
        {
            get => _editTrader;
            set { _editTrader = value; OnPropertyChanged(); }
        }

        public string EditTraderLocation
        {
            get => _editTraderLocation;
            set { _editTraderLocation = value; OnPropertyChanged(); }
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

        // API key for drawer
        public string? DrawerApiKey => _selectedBrand?.ApiKey;

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ShowApiKeyCommand { get; }
        public ICommand RegenerateKeyCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }

        private void SetDrawerMode(BrandDrawerMode mode)
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
            _editBrandId = null;
            EditBrandName = string.Empty;
            EditLogoUrl = string.Empty;
            EditSubBrand = string.Empty;
            EditParentCompany = string.Empty;
            EditTrader = string.Empty;
            EditTraderLocation = string.Empty;
            EditLei = string.Empty;
            EditGs1 = string.Empty;
            EditIsActive = true;
            StatusMessage = string.Empty;
            SetDrawerMode(BrandDrawerMode.New);
        }

        private void OpenEditDrawer(BrandSummary? brand)
        {
            if (brand == null) return;
            SelectedBrand = brand;
            _editBrandId = brand.Id;
            EditBrandName = brand.BrandName ?? string.Empty;
            EditLogoUrl = brand.LogoUrl ?? string.Empty;
            EditSubBrand = brand.SubBrand ?? string.Empty;
            EditParentCompany = brand.ParentCompany ?? string.Empty;
            EditTrader = brand.Trader ?? string.Empty;
            EditTraderLocation = brand.TraderLocation ?? string.Empty;
            EditLei = brand.Lei ?? string.Empty;
            EditGs1 = brand.Gs1CompanyPrefix ?? string.Empty;
            EditIsActive = brand.IsActive == 1;
            StatusMessage = string.Empty;
            SetDrawerMode(BrandDrawerMode.Edit);
        }

        private void OpenApiKeyDrawer(BrandSummary? brand)
        {
            if (brand == null) return;
            SelectedBrand = brand;
            OnPropertyChanged(nameof(DrawerApiKey));
            SetDrawerMode(BrandDrawerMode.ApiKey);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(BrandDrawerMode.None);
        }

        private async Task SaveAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["brand_name"] = EditBrandName.Trim(),
                    ["logo_url"] = NullIfEmpty(EditLogoUrl),
                    ["sub_brand"] = NullIfEmpty(EditSubBrand),
                    ["parent_company"] = NullIfEmpty(EditParentCompany),
                    ["trader"] = NullIfEmpty(EditTrader),
                    ["trader_location"] = NullIfEmpty(EditTraderLocation),
                    ["lei"] = NullIfEmpty(EditLei),
                    ["gs1_company_prefix"] = NullIfEmpty(EditGs1),
                };

                payload["_is_active"] = EditIsActive ? 1 : 0;

                string? result;
                if (_drawerMode == BrandDrawerMode.New)
                {
                    result = await _apiClient.PostAsync("/api/admin/brands", payload);
                }
                else
                {
                    result = await _apiClient.PutAsync($"/api/admin/brands/{_editBrandId}", payload);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadBrandsAsync();
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

        private async Task DeleteBrandAsync(BrandSummary? brand)
        {
            if (brand == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{brand.BrandName}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteAsync($"/api/admin/brands/{brand.Id}");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBrandsAsync();
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

        private async Task ToggleActiveAsync(BrandSummary? brand)
        {
            if (brand == null) return;

            var newActive = brand.IsActive == 1 ? 0 : 1;
            var payload = new Dictionary<string, object?>
            {
                ["brand_name"] = brand.BrandName,
                ["_is_active"] = newActive
            };

            try
            {
                var json = await _apiClient.PutAsync($"/api/admin/brands/{brand.Id}", payload);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBrandsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminBrands] ToggleActive error: {ex.Message}");
            }
        }

        private async Task RegenerateKeyAsync(BrandSummary? brand)
        {
            if (brand == null) return;

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
                var json = await _apiClient.PostAsync($"/api/admin/brands/{brand.Id}/regenerate-key", new { });
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
                            brand.ApiKey = newKey;
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
            var selected = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _statusFilterOptions = StatusFilterOption.All;
            foreach (var opt in _statusFilterOptions)
            {
                opt.IsSelected = selected.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(StatusFilterOptions));
            ApplyFilter();
        }

        private async Task LoadBrandsAsync()
        {
            try
            {
                var json = await _apiClient.GetRawAsync("/api/admin/brands");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BrandSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allBrands = items;
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

                // Compute supplier counts per brand
                var counts = _allRelations.GroupBy(r => r.BrandId).ToDictionary(g => g.Key, g => g.Count());
                foreach (var brand in _allBrands)
                    brand.SupplierCount = counts.TryGetValue(brand.Id, out var c) ? c : 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminBrands] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadBrandsAsync()
        {
            _allBrands.Clear();
            Brands.Clear();
            await LoadBrandsAsync();
        }

        private void ApplyFilter()
        {
            Brands.Clear();
            var filter = _searchText.Trim();
            IEnumerable<BrandSummary> filtered = _allBrands;

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(b => b.BrandName != null &&
                    b.BrandName.Contains(filter, StringComparison.OrdinalIgnoreCase));

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(b =>
                    (activeFilters.Contains("active") && b.IsActive == 1) ||
                    (activeFilters.Contains("inactive") && b.IsActive != 1));

            foreach (var b in filtered)
                Brands.Add(b);

            OnPropertyChanged(nameof(HasBrands));
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
