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
    public class AdminRelationsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<BrandSummary> _allBrands = new();
        private List<SupplierDetail> _allSuppliers = new();
        private List<RelationEntry> _allRelations = new();
        private string _searchText = string.Empty;
        private List<StatusFilterOption> _statusFilterOptions = StatusFilterOption.All;

        private bool _isDrawerOpen;
        private string _statusMessage = string.Empty;
        private bool _isSaving;
        private string _drawerTitle = string.Empty;
        private bool _isBrandLocked;

        // Drawer fields
        private BrandSummary? _drawerSelectedBrand;
        private SupplierDetail? _drawerSelectedSupplier;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AdminRelationsViewModel()
        {
            _apiClient = App.ApiClient;

            AddSupplierForBrandCommand = new RelayCommand(p => OpenDrawerForBrand(p as BrandRelationGroup));
            DeleteCommand = new RelayCommand(async p => await DeleteRelationAsync(p as RelationEntry));
            ToggleActiveCommand = new RelayCommand(async p => await ToggleActiveAsync(p as RelationEntry));
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => DrawerSelectedBrand != null && DrawerSelectedSupplier != null && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());

            var saved = SettingsService.LoadFilter("admin_relations");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadDataAsync();
        }

        public ObservableCollection<BrandRelationGroup> GroupedRelations { get; } = new();
        public ObservableCollection<SupplierDetail> FilteredSupplierOptions { get; } = new();
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
                SettingsService.SaveFilter("admin_relations", string.Join(",", sel));
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

        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set { _isDrawerOpen = value; OnPropertyChanged(); }
        }

        public bool HasRelations => GroupedRelations.Count > 0;

        public string DrawerTitle
        {
            get => _drawerTitle;
            set { _drawerTitle = value; OnPropertyChanged(); }
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

        public BrandSummary? DrawerSelectedBrand
        {
            get => _drawerSelectedBrand;
            set { _drawerSelectedBrand = value; OnPropertyChanged(); }
        }

        public SupplierDetail? DrawerSelectedSupplier
        {
            get => _drawerSelectedSupplier;
            set { _drawerSelectedSupplier = value; OnPropertyChanged(); }
        }

        public bool IsBrandLocked
        {
            get => _isBrandLocked;
            set { _isBrandLocked = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddSupplierForBrandCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }

        public void OpenDrawerForBrand(BrandRelationGroup? group)
        {
            if (group == null) return;

            var brand = _allBrands.FirstOrDefault(b => b.Id == group.BrandId);
            if (brand == null) return;

            DrawerSelectedBrand = brand;
            DrawerSelectedSupplier = null;
            IsBrandLocked = true;
            StatusMessage = string.Empty;

            var addText = Application.Current.TryFindResource("Action_AddSupplier") as string ?? "Lägg till leverantör";
            DrawerTitle = $"{addText} \u2013 {group.BrandName}";

            // Filter out suppliers already linked to this brand
            var linkedSupplierIds = _allRelations
                .Where(r => r.BrandId == group.BrandId)
                .Select(r => r.SupplierId)
                .ToHashSet();

            FilteredSupplierOptions.Clear();
            foreach (var s in _allSuppliers.Where(s => !linkedSupplierIds.Contains(s.Id)))
                FilteredSupplierOptions.Add(s);

            IsDrawerOpen = true;
        }

        private void CloseDrawer()
        {
            IsDrawerOpen = false;
            IsBrandLocked = false;
        }

        private async Task SaveAsync()
        {
            if (DrawerSelectedBrand == null || DrawerSelectedSupplier == null) return;

            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["brand_id"] = DrawerSelectedBrand.Id,
                    ["supplier_id"] = DrawerSelectedSupplier.Id
                };

                var result = await _apiClient.PostAsync("/api/admin/relations", payload);
                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadDataAsync();
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

        private async Task DeleteRelationAsync(RelationEntry? relation)
        {
            if (relation == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{relation.BrandName} \u2194 {relation.SupplierName}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteAsync($"/api/admin/relations/{relation.Id}");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadDataAsync();
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

        private async Task ToggleActiveAsync(RelationEntry? relation)
        {
            if (relation == null) return;

            var newActive = relation.IsActive == 1 ? 0 : 1;
            var payload = new Dictionary<string, object?>
            {
                ["_is_active"] = newActive
            };

            try
            {
                var json = await _apiClient.PutAsync($"/api/admin/relations/{relation.Id}", payload);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminRelations] ToggleActive error: {ex.Message}");
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

        private async Task LoadDataAsync()
        {
            try
            {
                var brandsJson = await _apiClient.GetRawAsync("/api/admin/brands");
                if (brandsJson != null)
                {
                    using var doc = JsonDocument.Parse(brandsJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BrandSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null) _allBrands = items;
                    }
                }

                var suppliersJson = await _apiClient.GetRawAsync("/api/admin/suppliers");
                if (suppliersJson != null)
                {
                    using var doc = JsonDocument.Parse(suppliersJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<SupplierDetail>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null) _allSuppliers = items;
                    }
                }

                var relationsJson = await _apiClient.GetRawAsync("/api/admin/relations");
                if (relationsJson != null)
                {
                    using var doc = JsonDocument.Parse(relationsJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<RelationEntry>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var brand = _allBrands.FirstOrDefault(b => b.Id == item.BrandId);
                                var supplier = _allSuppliers.FirstOrDefault(s => s.Id == item.SupplierId);
                                item.BrandName = brand?.BrandName ?? $"(Brand #{item.BrandId})";
                                item.SupplierName = supplier?.SupplierName ?? $"(Supplier #{item.SupplierId})";
                                item.SupplierLocation = supplier?.SupplierLocation;
                            }
                            _allRelations = items;
                        }
                    }
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminRelations] Load error: {ex.Message}");
            }
        }

        private async Task ReloadDataAsync()
        {
            _allRelations.Clear();
            GroupedRelations.Clear();
            await LoadDataAsync();
        }

        private void ApplyFilter()
        {
            GroupedRelations.Clear();
            var filter = _searchText.Trim();

            IEnumerable<RelationEntry> filtered = _allRelations;

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(r =>
                    (r.BrandName != null && r.BrandName.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (r.SupplierName != null && r.SupplierName.Contains(filter, StringComparison.OrdinalIgnoreCase)));

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(r =>
                    (activeFilters.Contains("active") && r.IsActive == 1) ||
                    (activeFilters.Contains("inactive") && r.IsActive != 1));

            var filteredList = filtered.ToList();

            var brandIds = filteredList.Select(r => r.BrandId).Distinct();

            foreach (var brandId in brandIds.OrderBy(id => id))
            {
                var brandRelations = filteredList.Where(r => r.BrandId == brandId).ToList();
                var brandName = brandRelations.FirstOrDefault()?.BrandName ?? $"(Brand #{brandId})";

                var group = new BrandRelationGroup
                {
                    BrandId = brandId,
                    BrandName = brandName
                };

                foreach (var r in brandRelations)
                    group.Relations.Add(r);

                GroupedRelations.Add(group);
            }

            if (string.IsNullOrEmpty(filter) && activeFilters.Count == 0)
            {
                var brandsWithRelations = GroupedRelations.Select(g => g.BrandId).ToHashSet();
                foreach (var brand in _allBrands.Where(b => !brandsWithRelations.Contains(b.Id)))
                {
                    GroupedRelations.Add(new BrandRelationGroup
                    {
                        BrandId = brand.Id,
                        BrandName = brand.BrandName ?? $"(Brand #{brand.Id})"
                    });
                }
            }

            OnPropertyChanged(nameof(HasRelations));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
