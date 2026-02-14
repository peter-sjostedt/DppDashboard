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
    public enum BatchDrawerMode { None, NewBatch, EditBatch, ManageMaterials, ViewItems }

    public class BrandBatchesViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<BatchSummary> _allBatches = new();
        private string _searchText = string.Empty;
        private List<BatchStatusFilterOption> _batchStatusFilterOptions = BatchStatusFilterOption.All;

        private BatchSummary? _selectedBatch;
        private BatchDrawerMode _drawerMode = BatchDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editBatchId;
        private string _editBatchNumber = string.Empty;
        private string _editPoNumber = string.Empty;
        private string _editQuantity = string.Empty;
        private string _editStatus = string.Empty;
        private string _editProductionDate = string.Empty;
        private ProductSummary? _editSelectedProduct;
        private SupplierSummary? _editSelectedSupplier;

        private string _generateItemCount = "1";
        private MaterialSummary? _selectedAvailableMaterial;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after batches are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public BrandBatchesViewModel()
        {
            _apiClient = App.ApiClient;

            AddBatchCommand = new RelayCommand(_ => OpenNewDrawer());
            EditBatchCommand = new RelayCommand(p => OpenEditDrawer(p as BatchSummary));
            DeleteBatchCommand = new RelayCommand(async p => await DeleteBatchAsync(p as BatchSummary));
            SaveBatchCommand = new RelayCommand(async _ => await SaveBatchAsync(), _ => !string.IsNullOrWhiteSpace(EditBatchNumber) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());
            ManageMaterialsCommand = new RelayCommand(async p => await OpenMaterialsDrawer(p as BatchSummary));
            AddMaterialCommand = new RelayCommand(async _ => await AddMaterialAsync());
            RemoveMaterialCommand = new RelayCommand(async p => await RemoveMaterialAsync(p as BatchMaterialInfo));
            ViewItemsCommand = new RelayCommand(async p => await OpenItemsDrawer(p as BatchSummary));
            GenerateItemsCommand = new RelayCommand(async _ => await GenerateItemsAsync());
            DeleteItemCommand = new RelayCommand(async p => await DeleteItemAsync(p as ItemInfo));

            var saved = SettingsService.LoadFilter("brand_batches");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _batchStatusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _batchStatusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadBatchesAsync();
        }

        // Collections
        public ObservableCollection<ProductBatchGroup> GroupedBatches { get; } = new();
        public ObservableCollection<ProductSummary> ProductOptions { get; } = new();
        public ObservableCollection<SupplierSummary> SupplierOptions { get; } = new();
        public ObservableCollection<BatchMaterialInfo> BatchMaterials { get; } = new();
        public ObservableCollection<MaterialSummary> AvailableMaterials { get; } = new();
        public ObservableCollection<ItemInfo> BatchItems { get; } = new();

        // Filter
        public List<BatchStatusFilterOption> BatchStatusFilterOptions
        {
            get => _batchStatusFilterOptions;
            private set { _batchStatusFilterOptions = value; OnPropertyChanged(); }
        }

        private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BatchStatusFilterOption.IsSelected))
            {
                ApplyFilter();
                var sel = _batchStatusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value);
                SettingsService.SaveFilter("brand_batches", string.Join(",", sel));
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

        public BatchSummary? SelectedBatch
        {
            get => _selectedBatch;
            set { _selectedBatch = value; OnPropertyChanged(); }
        }

        // Computed bools
        public bool IsDrawerOpen => _drawerMode != BatchDrawerMode.None;
        public bool IsBatchDrawer => _drawerMode == BatchDrawerMode.NewBatch || _drawerMode == BatchDrawerMode.EditBatch;
        public bool IsMaterialsDrawer => _drawerMode == BatchDrawerMode.ManageMaterials;
        public bool IsItemsDrawer => _drawerMode == BatchDrawerMode.ViewItems;
        public bool HasBatches => GroupedBatches.Count > 0;
        public bool ShowProductDropdown => _drawerMode == BatchDrawerMode.NewBatch;

        public int TotalCount => _allBatches.Count;

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                BatchDrawerMode.NewBatch => Application.Current.TryFindResource("Drawer_NewBatch") as string ?? "Ny batch",
                BatchDrawerMode.EditBatch => Application.Current.TryFindResource("Drawer_EditBatch") as string ?? "Redigera batch",
                BatchDrawerMode.ManageMaterials => Application.Current.TryFindResource("Drawer_ManageMaterials") as string ?? "Hantera material",
                BatchDrawerMode.ViewItems => Application.Current.TryFindResource("Drawer_ViewItems") as string ?? "Artiklar",
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
        public string EditBatchNumber
        {
            get => _editBatchNumber;
            set { _editBatchNumber = value; OnPropertyChanged(); }
        }

        public string EditPoNumber
        {
            get => _editPoNumber;
            set { _editPoNumber = value; OnPropertyChanged(); }
        }

        public string EditQuantity
        {
            get => _editQuantity;
            set { _editQuantity = value; OnPropertyChanged(); }
        }

        public string EditStatus
        {
            get => _editStatus;
            set { _editStatus = value; OnPropertyChanged(); }
        }

        public string EditProductionDate
        {
            get => _editProductionDate;
            set { _editProductionDate = value; OnPropertyChanged(); }
        }

        public ProductSummary? EditSelectedProduct
        {
            get => _editSelectedProduct;
            set { _editSelectedProduct = value; OnPropertyChanged(); }
        }

        public SupplierSummary? EditSelectedSupplier
        {
            get => _editSelectedSupplier;
            set { _editSelectedSupplier = value; OnPropertyChanged(); }
        }

        public MaterialSummary? SelectedAvailableMaterial
        {
            get => _selectedAvailableMaterial;
            set { _selectedAvailableMaterial = value; OnPropertyChanged(); }
        }

        public string GenerateItemCount
        {
            get => _generateItemCount;
            set { _generateItemCount = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddBatchCommand { get; }
        public ICommand EditBatchCommand { get; }
        public ICommand DeleteBatchCommand { get; }
        public ICommand SaveBatchCommand { get; }
        public ICommand CancelDrawerCommand { get; }
        public ICommand ManageMaterialsCommand { get; }
        public ICommand AddMaterialCommand { get; }
        public ICommand RemoveMaterialCommand { get; }
        public ICommand ViewItemsCommand { get; }
        public ICommand GenerateItemsCommand { get; }
        public ICommand DeleteItemCommand { get; }

        private void SetDrawerMode(BatchDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsBatchDrawer));
            OnPropertyChanged(nameof(IsMaterialsDrawer));
            OnPropertyChanged(nameof(IsItemsDrawer));
            OnPropertyChanged(nameof(ShowProductDropdown));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private async void OpenNewDrawer()
        {
            _editBatchId = null;
            EditBatchNumber = string.Empty;
            EditPoNumber = string.Empty;
            EditQuantity = string.Empty;
            EditStatus = string.Empty;
            EditProductionDate = string.Empty;
            EditSelectedProduct = null;
            EditSelectedSupplier = null;
            StatusMessage = string.Empty;

            await LoadProductOptionsAsync();
            await LoadSupplierOptionsAsync();

            SetDrawerMode(BatchDrawerMode.NewBatch);
        }

        private async void OpenEditDrawer(BatchSummary? batch)
        {
            if (batch == null) return;

            SelectedBatch = batch;
            _editBatchId = batch.Id;
            EditBatchNumber = batch.BatchNumber ?? string.Empty;
            EditPoNumber = batch.PoNumber ?? string.Empty;
            EditQuantity = batch.Quantity?.ToString() ?? string.Empty;
            EditStatus = batch.Status ?? string.Empty;
            EditProductionDate = batch.ProductionDate ?? string.Empty;
            StatusMessage = string.Empty;

            await LoadSupplierOptionsAsync();
            EditSelectedSupplier = SupplierOptions.FirstOrDefault(s => s.Id == batch.SupplierId);

            SetDrawerMode(BatchDrawerMode.EditBatch);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(BatchDrawerMode.None);
        }

        private async Task SaveBatchAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var tenantKey = App.Session?.BrandKey;
                if (string.IsNullOrEmpty(tenantKey))
                {
                    StatusMessage = "Ingen API-nyckel tillgänglig";
                    return;
                }

                var payload = new Dictionary<string, object?>
                {
                    ["batch_number"] = EditBatchNumber.Trim(),
                    ["po_number"] = NullIfEmpty(EditPoNumber),
                    ["supplier_id"] = EditSelectedSupplier?.Id,
                    ["quantity"] = int.TryParse(EditQuantity, out var qty) ? qty : null,
                    ["_status"] = NullIfEmpty(EditStatus),
                    ["production_date"] = NullIfEmpty(EditProductionDate)
                };

                string? result;
                if (_drawerMode == BatchDrawerMode.NewBatch)
                {
                    if (EditSelectedProduct == null)
                    {
                        StatusMessage = Application.Current.TryFindResource("Msg_SelectProduct") as string ?? "Välj en produkt";
                        return;
                    }
                    result = await _apiClient.PostWithTenantKeyAsync($"/api/products/{EditSelectedProduct.Id}/batches", payload, tenantKey);
                }
                else
                {
                    result = await _apiClient.PutWithTenantKeyAsync($"/api/batches/{_editBatchId}", payload, tenantKey);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadBatchesAsync();
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

        private async Task DeleteBatchAsync(BatchSummary? batch)
        {
            if (batch == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{batch.BatchNumber}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/batches/{batch.Id}", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchesAsync();
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

        private async Task OpenMaterialsDrawer(BatchSummary? batch)
        {
            if (batch == null) return;

            SelectedBatch = batch;
            SelectedAvailableMaterial = null;
            StatusMessage = string.Empty;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            // Load batch materials
            try
            {
                BatchMaterials.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}/materials", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BatchMaterialInfo>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                BatchMaterials.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load batch materials error: {ex.Message}");
            }

            // Load available materials from supplier
            try
            {
                AvailableMaterials.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/suppliers/{batch.SupplierId}/materials", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                AvailableMaterials.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load available materials error: {ex.Message}");
            }

            SetDrawerMode(BatchDrawerMode.ManageMaterials);
        }

        private async Task AddMaterialAsync()
        {
            if (SelectedBatch == null || SelectedAvailableMaterial == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["factory_material_id"] = SelectedAvailableMaterial.Id,
                    ["component"] = "main"
                };

                var json = await _apiClient.PostWithTenantKeyAsync($"/api/batches/{SelectedBatch.Id}/materials", payload, tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchMaterialsAsync();
                        SelectedAvailableMaterial = null;
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusMessage = $"Fel: {err.GetString()}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
            }
        }

        private async Task RemoveMaterialAsync(BatchMaterialInfo? material)
        {
            if (material == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{material.MaterialName}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/batch-materials/{material.Id}", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchMaterialsAsync();
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

        private async Task OpenItemsDrawer(BatchSummary? batch)
        {
            if (batch == null) return;

            SelectedBatch = batch;
            GenerateItemCount = "1";
            StatusMessage = string.Empty;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                BatchItems.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}/items", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<ItemInfo>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                BatchItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load items error: {ex.Message}");
            }

            SetDrawerMode(BatchDrawerMode.ViewItems);
        }

        private async Task GenerateItemsAsync()
        {
            if (SelectedBatch == null) return;

            if (!int.TryParse(GenerateItemCount, out var count) || count < 1)
            {
                StatusMessage = Application.Current.TryFindResource("Msg_InvalidCount") as string ?? "Ange ett giltigt antal";
                return;
            }

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["count"] = count
                };

                var json = await _apiClient.PostWithTenantKeyAsync($"/api/batches/{SelectedBatch.Id}/items/bulk", payload, tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchItemsAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusMessage = $"Fel: {err.GetString()}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
            }
        }

        private async Task DeleteItemAsync(ItemInfo? item)
        {
            if (item == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{item.UniqueProductId}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/items/{item.Id}", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchItemsAsync();
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

        private void OnLanguageChanged()
        {
            var selectedValues = _batchStatusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _batchStatusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _batchStatusFilterOptions = BatchStatusFilterOption.All;
            foreach (var opt in _batchStatusFilterOptions)
            {
                opt.IsSelected = selectedValues.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(BatchStatusFilterOptions));
            ApplyFilter();
        }

        private async Task LoadBatchesAsync()
        {
            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/batches", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BatchSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allBatches = items;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadBatchesAsync()
        {
            _allBatches.Clear();
            GroupedBatches.Clear();
            await LoadBatchesAsync();
        }

        private async Task ReloadBatchMaterialsAsync()
        {
            if (SelectedBatch == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                BatchMaterials.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{SelectedBatch.Id}/materials", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BatchMaterialInfo>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                BatchMaterials.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Reload batch materials error: {ex.Message}");
            }
        }

        private async Task ReloadBatchItemsAsync()
        {
            if (SelectedBatch == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                BatchItems.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{SelectedBatch.Id}/items", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<ItemInfo>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                BatchItems.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Reload items error: {ex.Message}");
            }
        }

        private async Task LoadProductOptionsAsync()
        {
            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                ProductOptions.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync("/api/products", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<ProductSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                ProductOptions.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load products error: {ex.Message}");
            }
        }

        private async Task LoadSupplierOptionsAsync()
        {
            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                SupplierOptions.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync("/api/suppliers", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<SupplierSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var item in items)
                                SupplierOptions.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandBatches] Load suppliers error: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            GroupedBatches.Clear();
            var filter = _searchText.Trim();
            IEnumerable<BatchSummary> filtered = _allBatches;

            if (!string.IsNullOrEmpty(filter))
            {
                filtered = filtered.Where(b =>
                    (b.BatchNumber != null && b.BatchNumber.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (b.ProductName != null && b.ProductName.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                    (b.SupplierName != null && b.SupplierName.Contains(filter, StringComparison.OrdinalIgnoreCase)));
            }

            var activeFilters = _batchStatusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(b => b.Status != null && activeFilters.Contains(b.Status));

            var groups = filtered
                .GroupBy(b => b.ProductId)
                .Select(g => new ProductBatchGroup
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName ?? string.Empty,
                    Batches = new ObservableCollection<BatchSummary>(g)
                });

            foreach (var group in groups)
                GroupedBatches.Add(group);

            OnPropertyChanged(nameof(HasBatches));
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
