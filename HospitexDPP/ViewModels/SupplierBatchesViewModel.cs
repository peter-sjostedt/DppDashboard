using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public enum SupplierBatchDrawerMode { None, New, Edit }

    public class SupplierBatchesViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly ApiClient _apiClient;
        private List<BatchSummary> _allBatches = new();
        private string _searchText = string.Empty;
        private List<BatchStatusFilterOption> _statusFilterOptions = BatchStatusFilterOption.All;

        private SupplierBatchDrawerMode _drawerMode = SupplierBatchDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;
        private bool _isLoading;

        // Drawer edit fields
        private int? _editBatchId;
        private string _editBatchNumber = string.Empty;
        private string _editProductionDate = string.Empty;
        private string _editQuantity = string.Empty;
        private string _editFacilityName = string.Empty;
        private string _editFacilityLocation = string.Empty;
        private string _editFacilityRegistry = string.Empty;
        private string _editFacilityIdentifier = string.Empty;
        private string _editCountryConfection = string.Empty;
        private string _editCountryDyeing = string.Empty;
        private string _editCountryWeaving = string.Empty;

        // Create-mode
        private PurchaseOrderSummary? _selectedPo;

        // Bulk-create items
        private string _bulkQuantity = string.Empty;
        private string _bulkSerialPrefix = "SN-";
        private VariantInfo? _selectedVariant;
        private MaterialSummary? _selectedMaterial;

        // Split batch
        private List<BatchMaterialInfo>? _pendingSplitMaterials;
        private int? _splitFromBatchId;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? OnDataChanged;

        public SupplierBatchesViewModel()
        {
            _apiClient = App.ApiClient;
            var saved = SettingsService.LoadFilter("supplier_batches");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;

            AddCommand = new RelayCommand(async _ => await OpenNewDrawerAsync());
            EditCommand = new RelayCommand(p => OpenEditDrawerAsync(p as BatchSummary));
            SaveCommand = new RelayCommand(async _ => await SaveBatchAsync(), _ => !string.IsNullOrWhiteSpace(EditBatchNumber) && !IsSaving);
            DeleteCommand = new RelayCommand(async p => await DeleteBatchAsync(p as BatchSummary));
            MarkCompletedCommand = new RelayCommand(async p => await MarkCompletedAsync(p as BatchSummary));
            SplitBatchCommand = new RelayCommand(async p => await SplitBatchAsync(p as BatchSummary));
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());
            AddMaterialCommand = new RelayCommand(async _ => await AddMaterialAsync(), _ => SelectedMaterial != null);
            RemoveMaterialCommand = new RelayCommand(async p => await RemoveMaterialAsync(p as BatchMaterialInfo));
            BulkCreateItemsCommand = new RelayCommand(async _ => await BulkCreateItemsAsync(), _ => SelectedVariant != null && !string.IsNullOrWhiteSpace(BulkQuantity));
            ToggleGroupCommand = new RelayCommand(p => { if (p is ItemVariantGroup g) g.IsExpanded = !g.IsExpanded; });
            RefreshCommand = new RelayCommand(async _ => await ReloadAsync());

            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadBatchesAsync();
        }

        // Collections
        public ObservableCollection<BatchSummary> Batches { get; } = new();
        public ObservableCollection<PurchaseOrderSummary> AvailablePurchaseOrders { get; } = new();
        public ObservableCollection<BatchMaterialInfo> BatchMaterials { get; } = new();
        public ObservableCollection<ItemVariantGroup> ItemGroups { get; } = new();
        public ObservableCollection<MaterialSummary> AvailableMaterials { get; } = new();
        public ObservableCollection<VariantInfo> AvailableVariants { get; } = new();

        // Filter properties
        public List<BatchStatusFilterOption> StatusFilterOptions
        {
            get => _statusFilterOptions;
            private set { _statusFilterOptions = value; OnPropertyChanged(); }
        }

        private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BatchStatusFilterOption.IsSelected))
            {
                ApplyFilter();
                var sel = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value);
                SettingsService.SaveFilter("supplier_batches", string.Join(",", sel));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        // Drawer state
        public bool IsDrawerOpen => _drawerMode != SupplierBatchDrawerMode.None;
        public bool IsEditMode => _drawerMode == SupplierBatchDrawerMode.Edit;
        public bool IsCreateMode => _drawerMode == SupplierBatchDrawerMode.New;
        public bool HasBatches => Batches.Count > 0;
        public bool HasMaterials => BatchMaterials.Count > 0;
        public bool HasItems => ItemGroups.Count > 0;
        public int TotalCount => _allBatches.Count;

        private BatchDetail? _selectedBatchDetail;
        public BatchDetail? SelectedBatchDetail
        {
            get => _selectedBatchDetail;
            private set
            {
                _selectedBatchDetail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemCountLabel));
            }
        }

        public int TotalItemCount => ItemGroups.Sum(g => g.Count);

        public string ItemCountLabel
        {
            get
            {
                var total = SelectedBatchDetail?.Quantity ?? 0;
                return $"{TotalItemCount} / {total}";
            }
        }

        public string DrawerTitle => _drawerMode switch
        {
            SupplierBatchDrawerMode.New => Application.Current.TryFindResource("Drawer_SupplierNewBatch") as string ?? "Ny batch",
            SupplierBatchDrawerMode.Edit => Application.Current.TryFindResource("Drawer_SupplierEditBatch") as string ?? "Redigera batch",
            _ => string.Empty
        };

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

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Edit properties
        public string EditBatchNumber
        {
            get => _editBatchNumber;
            set { _editBatchNumber = value; OnPropertyChanged(); }
        }

        public string EditProductionDate
        {
            get => _editProductionDate;
            set { _editProductionDate = value; OnPropertyChanged(); }
        }

        public string EditQuantity
        {
            get => _editQuantity;
            set { _editQuantity = value; OnPropertyChanged(); }
        }

        public string EditFacilityName
        {
            get => _editFacilityName;
            set { _editFacilityName = value; OnPropertyChanged(); }
        }

        public string EditFacilityLocation
        {
            get => _editFacilityLocation;
            set { _editFacilityLocation = value; OnPropertyChanged(); }
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

        // Create-mode: selected PO
        public PurchaseOrderSummary? SelectedPo
        {
            get => _selectedPo;
            set { _selectedPo = value; OnPropertyChanged(); }
        }

        // Bulk-create items
        public string BulkQuantity
        {
            get => _bulkQuantity;
            set { _bulkQuantity = value; OnPropertyChanged(); }
        }

        public string BulkSerialPrefix
        {
            get => _bulkSerialPrefix;
            set { _bulkSerialPrefix = value; OnPropertyChanged(); }
        }

        public VariantInfo? SelectedVariant
        {
            get => _selectedVariant;
            set { _selectedVariant = value; OnPropertyChanged(); }
        }

        // Add material dropdown
        public MaterialSummary? SelectedMaterial
        {
            get => _selectedMaterial;
            set { _selectedMaterial = value; OnPropertyChanged(); }
        }

        // Split mode
        public bool IsSplitMode => _pendingSplitMaterials != null;

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand SplitBatchCommand { get; }
        public ICommand CancelDrawerCommand { get; }
        public ICommand AddMaterialCommand { get; }
        public ICommand RemoveMaterialCommand { get; }
        public ICommand BulkCreateItemsCommand { get; }
        public ICommand ToggleGroupCommand { get; }
        public ICommand RefreshCommand { get; }

        // --- Drawer management ---

        private void SetDrawerMode(SupplierBatchDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private async Task OpenNewDrawerAsync()
        {
            _editBatchId = null;
            EditBatchNumber = string.Empty;
            EditProductionDate = string.Empty;
            EditQuantity = string.Empty;
            EditFacilityName = string.Empty;
            EditFacilityLocation = string.Empty;
            EditFacilityRegistry = string.Empty;
            EditFacilityIdentifier = string.Empty;
            EditCountryConfection = string.Empty;
            EditCountryDyeing = string.Empty;
            EditCountryWeaving = string.Empty;
            SelectedPo = null;
            StatusMessage = string.Empty;
            BatchMaterials.Clear();
            ItemGroups.Clear();
            AvailableVariants.Clear();
            OnPropertyChanged(nameof(HasMaterials));
            OnPropertyChanged(nameof(HasItems));

            // Load accepted POs
            AvailablePurchaseOrders.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/purchase-orders", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var arr))
                    {
                        var items = JsonSerializer.Deserialize<List<PurchaseOrderSummary>>(arr.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var po in items.Where(p => p.Status == "accepted"))
                                AvailablePurchaseOrders.Add(po);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] LoadPOs error: {ex.Message}");
            }

            SetDrawerMode(SupplierBatchDrawerMode.New);
        }

        private async void OpenEditDrawerAsync(BatchSummary? batch)
        {
            if (batch == null) return;
            _editBatchId = batch.Id;
            StatusMessage = string.Empty;

            try
            {
                // Load batch detail
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<BatchDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            SelectedBatchDetail = detail;
                            EditBatchNumber = detail.BatchNumber;
                            EditProductionDate = detail.ProductionDate ?? string.Empty;
                            EditQuantity = detail.Quantity?.ToString() ?? string.Empty;
                            EditFacilityName = detail.FacilityName ?? string.Empty;
                            EditFacilityLocation = detail.FacilityLocation ?? string.Empty;
                            EditFacilityRegistry = detail.FacilityRegistry ?? string.Empty;
                            EditFacilityIdentifier = detail.FacilityIdentifier ?? string.Empty;
                            EditCountryConfection = detail.CountryConfection ?? string.Empty;
                            EditCountryDyeing = detail.CountryDyeing ?? string.Empty;
                            EditCountryWeaving = detail.CountryWeaving ?? string.Empty;

                            // Materials from detail response
                            BatchMaterials.Clear();
                            if (detail.Materials != null)
                                foreach (var m in detail.Materials) BatchMaterials.Add(m);
                            OnPropertyChanged(nameof(HasMaterials));
                        }
                    }
                }

                // Load items (grouped by variant)
                ItemGroups.Clear();
                var itemsJson = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}/items", App.Session!.SupplierKey!);
                if (itemsJson != null)
                {
                    using var doc = JsonDocument.Parse(itemsJson);
                    if (doc.RootElement.TryGetProperty("data", out var arr))
                    {
                        var items = JsonSerializer.Deserialize<List<ItemInfo>>(arr.GetRawText(), JsonOptions);
                        if (items != null) GroupItems(items);
                    }
                }
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(ItemCountLabel));

                // Load available materials for dropdown
                await LoadAvailableMaterialsAsync();

                // Load available variants for bulk-create
                if (SelectedBatchDetail != null)
                    await LoadAvailableVariantsAsync(SelectedBatchDetail.ProductId);

                // Reset bulk fields
                BulkQuantity = string.Empty;
                BulkSerialPrefix = "SN-";
                SelectedVariant = null;
                SelectedMaterial = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] OpenEditDrawer error: {ex.Message}");
            }

            SetDrawerMode(SupplierBatchDrawerMode.Edit);
        }

        private void CloseDrawer()
        {
            _pendingSplitMaterials = null;
            _splitFromBatchId = null;
            OnPropertyChanged(nameof(IsSplitMode));
            SetDrawerMode(SupplierBatchDrawerMode.None);
            SelectedBatchDetail = null;
        }

        // --- CRUD ---

        private async Task SaveBatchAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                int? qty = null;
                if (int.TryParse(EditQuantity, out var q)) qty = q;

                var payload = new Dictionary<string, object?>
                {
                    ["batch_number"] = EditBatchNumber.Trim(),
                    ["production_date"] = NullIfEmpty(EditProductionDate),
                    ["quantity"] = qty,
                    ["facility_name"] = NullIfEmpty(EditFacilityName),
                    ["facility_location"] = NullIfEmpty(EditFacilityLocation),
                    ["facility_registry"] = NullIfEmpty(EditFacilityRegistry),
                    ["facility_identifier"] = NullIfEmpty(EditFacilityIdentifier),
                    ["country_of_origin_confection"] = NullIfEmpty(EditCountryConfection),
                    ["country_of_origin_dyeing"] = NullIfEmpty(EditCountryDyeing),
                    ["country_of_origin_weaving"] = NullIfEmpty(EditCountryWeaving),
                };

                string? result;
                if (_drawerMode == SupplierBatchDrawerMode.New)
                {
                    if (SelectedPo == null)
                    {
                        StatusMessage = "Välj en inköpsorder";
                        IsSaving = false;
                        return;
                    }
                    result = await _apiClient.PostWithTenantKeyAsync(
                        $"/api/purchase-orders/{SelectedPo.Id}/batches", payload, App.Session!.SupplierKey!);
                }
                else
                {
                    result = await _apiClient.PutWithTenantKeyAsync(
                        $"/api/batches/{_editBatchId}", payload, App.Session!.SupplierKey!);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        // If this was a split, copy materials to the new batch
                        if (_pendingSplitMaterials != null && _pendingSplitMaterials.Count > 0
                            && doc.RootElement.TryGetProperty("data", out var newData)
                            && newData.TryGetProperty("id", out var newIdEl))
                        {
                            var newBatchId = newIdEl.GetInt32();
                            foreach (var mat in _pendingSplitMaterials)
                            {
                                try
                                {
                                    var matPayload = new Dictionary<string, object?>
                                    {
                                        ["factory_material_id"] = mat.FactoryMaterialId,
                                        ["component"] = mat.Component ?? "Body fabric"
                                    };
                                    await _apiClient.PostWithTenantKeyAsync(
                                        $"/api/batches/{newBatchId}/materials", matPayload, App.Session!.SupplierKey!);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[SupplierBatches] CopySplitMaterial error: {ex.Message}");
                                }
                            }
                        }

                        CloseDrawer();
                        await ReloadAsync();
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

            var confirmText = Application.Current.TryFindResource("Confirm_DeleteBatch") as string
                ?? "Ta bort denna batch?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{batch.BatchNumber}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/batches/{batch.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadAsync();
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

        private async Task MarkCompletedAsync(BatchSummary? batch)
        {
            if (batch == null || batch.Status != "in_production") return;

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["_status"] = "completed"
                };
                var json = await _apiClient.PutWithTenantKeyAsync($"/api/batches/{batch.Id}", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] MarkCompleted error: {ex.Message}");
            }
        }

        // --- Split batch ---

        private async Task SplitBatchAsync(BatchSummary? batch)
        {
            if (batch == null || batch.Status != "in_production") return;

            var confirmText = Application.Current.TryFindResource("Confirm_SplitBatch") as string
                ?? "Vill du splittra denna batch?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{batch.BatchNumber}",
                Application.Current.TryFindResource("Action_SplitBatch") as string ?? "Splittra batch",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // 1. Load batch detail to get facility info + materials
                BatchDetail? detail = null;
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                        detail = JsonSerializer.Deserialize<BatchDetail>(data.GetRawText(), JsonOptions);
                }
                if (detail == null) return;

                // 2. Mark current batch as completed
                var markPayload = new Dictionary<string, object?> { ["_status"] = "completed" };
                await _apiClient.PutWithTenantKeyAsync($"/api/batches/{batch.Id}", markPayload, App.Session!.SupplierKey!);

                // 3. Store materials for copying after new batch is created
                _pendingSplitMaterials = detail.Materials?.ToList();
                _splitFromBatchId = batch.Id;
                OnPropertyChanged(nameof(IsSplitMode));

                // 4. Open create drawer prefilled with old batch's settings
                _editBatchId = null;
                EditBatchNumber = GenerateSplitBatchNumber(detail.BatchNumber);
                EditProductionDate = detail.ProductionDate ?? string.Empty;
                EditQuantity = detail.Quantity?.ToString() ?? string.Empty;
                EditFacilityName = detail.FacilityName ?? string.Empty;
                EditFacilityLocation = detail.FacilityLocation ?? string.Empty;
                EditFacilityRegistry = detail.FacilityRegistry ?? string.Empty;
                EditFacilityIdentifier = detail.FacilityIdentifier ?? string.Empty;
                EditCountryConfection = detail.CountryConfection ?? string.Empty;
                EditCountryDyeing = detail.CountryDyeing ?? string.Empty;
                EditCountryWeaving = detail.CountryWeaving ?? string.Empty;
                StatusMessage = string.Empty;
                BatchMaterials.Clear();
                ItemGroups.Clear();
                AvailableVariants.Clear();
                OnPropertyChanged(nameof(HasMaterials));
                OnPropertyChanged(nameof(HasItems));

                // 5. Load accepted POs and pre-select the same PO
                AvailablePurchaseOrders.Clear();
                try
                {
                    var poJson = await _apiClient.GetWithTenantKeyAsync("/api/purchase-orders", App.Session!.SupplierKey!);
                    if (poJson != null)
                    {
                        using var doc = JsonDocument.Parse(poJson);
                        if (doc.RootElement.TryGetProperty("data", out var arr))
                        {
                            var items = JsonSerializer.Deserialize<List<PurchaseOrderSummary>>(arr.GetRawText(), JsonOptions);
                            if (items != null)
                                foreach (var po in items.Where(p => p.Status == "accepted"))
                                    AvailablePurchaseOrders.Add(po);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SupplierBatches] SplitBatch LoadPOs error: {ex.Message}");
                }

                // Pre-select the same PO
                SelectedPo = AvailablePurchaseOrders.FirstOrDefault(p => p.Id == detail.PurchaseOrderId);

                SetDrawerMode(SupplierBatchDrawerMode.New);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] SplitBatch error: {ex.Message}");
            }
        }

        private static string GenerateSplitBatchNumber(string? original)
        {
            if (string.IsNullOrEmpty(original)) return "B";

            var last = original[^1];
            if (last >= 'A' && last < 'Z')
                return original[..^1] + (char)(last + 1);

            return original + "B";
        }

        // --- Materials management ---

        private async Task AddMaterialAsync()
        {
            if (_editBatchId == null || SelectedMaterial == null) return;

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["factory_material_id"] = SelectedMaterial.Id,
                    ["component"] = "Body fabric"
                };
                var json = await _apiClient.PostWithTenantKeyAsync(
                    $"/api/batches/{_editBatchId}/materials", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        SelectedMaterial = null;
                        await ReloadBatchMaterialsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] AddMaterial error: {ex.Message}");
            }
        }

        private async Task RemoveMaterialAsync(BatchMaterialInfo? mat)
        {
            if (mat == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_RemoveMaterial") as string
                ?? "Ta bort tygkoppling?";
            var result = MessageBox.Show(confirmText,
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/batch-materials/{mat.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadBatchMaterialsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] RemoveMaterial error: {ex.Message}");
            }
        }

        // --- Bulk-create items ---

        private async Task BulkCreateItemsAsync()
        {
            if (_editBatchId == null || SelectedVariant == null) return;
            if (!int.TryParse(BulkQuantity, out var qty) || qty <= 0) return;

            IsSaving = true;
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["product_variant_id"] = SelectedVariant.Id,
                    ["quantity"] = qty,
                    ["serial_prefix"] = BulkSerialPrefix?.Trim() ?? "SN-"
                };
                var json = await _apiClient.PostWithTenantKeyAsync(
                    $"/api/batches/{_editBatchId}/items/bulk", payload, App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        BulkQuantity = string.Empty;
                        await ReloadBatchItemsAsync();
                        await ReloadAsync(); // Update item_count in list
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] BulkCreate error: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        // --- Data loading ---

        private async Task LoadBatchesAsync()
        {
            IsLoading = true;
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/batches", App.Session!.SupplierKey!);
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
                Debug.WriteLine($"[SupplierBatches] Load error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadAsync()
        {
            _allBatches.Clear();
            Batches.Clear();
            OnPropertyChanged(nameof(HasBatches));
            await LoadBatchesAsync();
        }

        private async Task ReloadBatchMaterialsAsync()
        {
            if (_editBatchId == null) return;

            BatchMaterials.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{_editBatchId}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<BatchDetail>(data.GetRawText(), JsonOptions);
                        if (detail?.Materials != null)
                            foreach (var m in detail.Materials) BatchMaterials.Add(m);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] ReloadMaterials error: {ex.Message}");
            }
            OnPropertyChanged(nameof(HasMaterials));
        }

        private async Task ReloadBatchItemsAsync()
        {
            if (_editBatchId == null) return;

            ItemGroups.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{_editBatchId}/items", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var arr))
                    {
                        var items = JsonSerializer.Deserialize<List<ItemInfo>>(arr.GetRawText(), JsonOptions);
                        if (items != null) GroupItems(items);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] ReloadItems error: {ex.Message}");
            }
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(ItemCountLabel));
        }

        private void GroupItems(List<ItemInfo> items)
        {
            ItemGroups.Clear();
            var groups = items
                .GroupBy(i => new { Size = i.Size ?? "", Color = i.ColorBrand ?? "" })
                .Select(g => new ItemVariantGroup
                {
                    Size = g.Key.Size,
                    ColorBrand = g.Key.Color,
                    Count = g.Count(),
                    SerialNumbers = g.Select(i => i.SerialNumber ?? i.Sgtin ?? $"#{i.Id}").OrderBy(s => s).ToList()
                })
                .OrderBy(g => g.Size)
                .ThenBy(g => g.ColorBrand)
                .ToList();

            foreach (var g in groups) ItemGroups.Add(g);
            OnPropertyChanged(nameof(TotalItemCount));
        }

        private async Task LoadAvailableMaterialsAsync()
        {
            AvailableMaterials.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/materials", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var arr))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSummary>>(arr.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var m in items.Where(m => m.IsActive == 1)) AvailableMaterials.Add(m);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] LoadMaterials error: {ex.Message}");
            }
        }

        private async Task LoadAvailableVariantsAsync(int productId)
        {
            AvailableVariants.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/variants", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var arr))
                    {
                        var items = JsonSerializer.Deserialize<List<VariantInfo>>(arr.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var v in items.Where(v => v.IsActive == 1)) AvailableVariants.Add(v);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBatches] LoadVariants error: {ex.Message}");
            }
        }

        // --- Filtering ---

        private void ApplyFilter()
        {
            Batches.Clear();
            var search = _searchText.Trim();
            IEnumerable<BatchSummary> filtered = _allBatches;

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(b =>
                    (b.BatchNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.PoNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.ProductName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.BrandName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(b => b.Status != null && activeFilters.Contains(b.Status));

            foreach (var b in filtered)
                Batches.Add(b);

            OnPropertyChanged(nameof(HasBatches));
        }

        private void OnLanguageChanged()
        {
            var selectedValues = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _statusFilterOptions = BatchStatusFilterOption.All;
            foreach (var opt in _statusFilterOptions)
            {
                opt.IsSelected = selectedValues.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(StatusFilterOptions));
            ApplyFilter();
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
