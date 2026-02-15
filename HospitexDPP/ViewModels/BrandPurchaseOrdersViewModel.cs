using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public enum PoDrawerMode { None, New, Edit }

    public class BrandPurchaseOrdersViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly ApiClient _apiClient;
        private List<PurchaseOrderSummary> _allOrders = new();
        private string _searchText = string.Empty;
        private List<PoStatusFilterOption> _statusFilterOptions = PoStatusFilterOption.All;

        private PurchaseOrderSummary? _selectedOrder;
        private PoDrawerMode _drawerMode = PoDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editOrderId;
        private string _editPoNumber = string.Empty;
        private string _editQuantity = string.Empty;
        private string _editRequestedDeliveryDate = string.Empty;
        private string _editStatus = "draft";
        private SupplierSummary? _editSelectedSupplier;
        private ProductSummary? _editSelectedProduct;

        // Order line add controls
        private VariantInfo? _selectedVariant;
        private string _newLineQuantity = "1";

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? OnDataChanged;

        public BrandPurchaseOrdersViewModel()
        {
            _apiClient = App.ApiClient;

            AddCommand = new RelayCommand(_ => OpenNewDrawer());
            EditCommand = new RelayCommand(p => OpenEditDrawer(p as PurchaseOrderSummary));
            DeleteCommand = new RelayCommand(async p => await DeleteOrderAsync(p as PurchaseOrderSummary));
            SaveCommand = new RelayCommand(async _ => await SaveOrderAsync(), _ => !string.IsNullOrWhiteSpace(EditPoNumber) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());
            SendCommand = new RelayCommand(async p => await SendOrderAsync(p as PurchaseOrderSummary));
            FulfillCommand = new RelayCommand(async p => await FulfillOrderAsync(p as PurchaseOrderSummary));
            CancelOrderCommand = new RelayCommand(async p => await CancelOrderAsync(p as PurchaseOrderSummary));
            AddLineCommand = new RelayCommand(async _ => await AddLineAsync());
            RemoveLineCommand = new RelayCommand(async p => await RemoveLineAsync(p as PurchaseOrderLine));

            // Load saved filter or set defaults
            var saved = SettingsService.LoadFilter("brand_pos");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    if (opt.Value != null) opt.IsSelected = selected.Contains(opt.Value);
            }
            else
            {
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = opt.Value is "draft" or "sent" or "accepted";
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;

            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadOrdersAsync();
        }

        // Collections
        public ObservableCollection<PurchaseOrderSummary> PurchaseOrders { get; } = new();
        public ObservableCollection<SupplierSummary> SupplierOptions { get; } = new();
        public ObservableCollection<ProductSummary> ProductOptions { get; } = new();
        public ObservableCollection<PurchaseOrderLine> OrderLines { get; } = new();
        public ObservableCollection<PoBatchInfo> OrderBatches { get; } = new();
        public ObservableCollection<VariantInfo> AvailableVariants { get; } = new();

        // Filter
        public List<PoStatusFilterOption> StatusFilterOptions
        {
            get => _statusFilterOptions;
            private set { _statusFilterOptions = value; OnPropertyChanged(); }
        }

        private void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PoStatusFilterOption.IsSelected))
            {
                ApplyFilter();
                var sel = _statusFilterOptions.Where(o => o.IsSelected && o.Value != null).Select(o => o.Value!);
                SettingsService.SaveFilter("brand_pos", string.Join(",", sel));
            }
        }

        /// <summary>Set status filter programmatically (from dashboard card).</summary>
        public void SetStatusFilter(string status)
        {
            foreach (var opt in _statusFilterOptions)
                opt.IsSelected = opt.Value == status;
        }

        // Properties
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public PurchaseOrderSummary? SelectedOrder
        {
            get => _selectedOrder;
            set { _selectedOrder = value; OnPropertyChanged(); }
        }

        public bool IsDrawerOpen => _drawerMode != PoDrawerMode.None;
        public bool IsEditMode => _drawerMode == PoDrawerMode.Edit;
        public bool IsCreateMode => _drawerMode == PoDrawerMode.New;
        public bool IsDraftMode => _editStatus == "draft" || IsCreateMode;
        public bool HasOrders => PurchaseOrders.Count > 0;
        public bool HasLines => OrderLines.Count > 0;
        public bool HasBatches => OrderBatches.Count > 0;
        public int TotalCount => _allOrders.Count;
        public int TotalLineQuantity => OrderLines.Sum(l => l.Quantity);

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                PoDrawerMode.New => Strings.Drawer_NewPo,
                PoDrawerMode.Edit => Strings.Drawer_EditPo,
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

        public string EditRequestedDeliveryDate
        {
            get => _editRequestedDeliveryDate;
            set { _editRequestedDeliveryDate = value; OnPropertyChanged(); }
        }

        public string EditStatus
        {
            get => _editStatus;
            set
            {
                _editStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDraftMode));
            }
        }

        public SupplierSummary? EditSelectedSupplier
        {
            get => _editSelectedSupplier;
            set { _editSelectedSupplier = value; OnPropertyChanged(); }
        }

        public ProductSummary? EditSelectedProduct
        {
            get => _editSelectedProduct;
            set
            {
                _editSelectedProduct = value;
                OnPropertyChanged();
                if (value != null && IsEditMode && IsDraftMode)
                    _ = LoadVariantsAsync(value.Id);
            }
        }

        public VariantInfo? SelectedVariant
        {
            get => _selectedVariant;
            set { _selectedVariant = value; OnPropertyChanged(); }
        }

        public string NewLineQuantity
        {
            get => _newLineQuantity;
            set { _newLineQuantity = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand FulfillCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand AddLineCommand { get; }
        public ICommand RemoveLineCommand { get; }

        private void SetDrawerMode(PoDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(IsDraftMode));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private async void OpenNewDrawer()
        {
            _editOrderId = null;
            EditPoNumber = string.Empty;
            EditQuantity = string.Empty;
            EditRequestedDeliveryDate = string.Empty;
            EditStatus = "draft";
            EditSelectedSupplier = null;
            EditSelectedProduct = null;
            OrderLines.Clear();
            OrderBatches.Clear();
            AvailableVariants.Clear();
            SelectedVariant = null;
            NewLineQuantity = "1";
            StatusMessage = string.Empty;

            await LoadSupplierOptionsAsync();
            await LoadProductOptionsAsync();

            SetDrawerMode(PoDrawerMode.New);
        }

        private async void OpenEditDrawer(PurchaseOrderSummary? order)
        {
            if (order == null) return;

            SelectedOrder = order;
            _editOrderId = order.Id;
            StatusMessage = string.Empty;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/purchase-orders/{order.Id}", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<PurchaseOrderDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            EditPoNumber = detail.PoNumber ?? string.Empty;
                            EditQuantity = detail.Quantity?.ToString() ?? string.Empty;
                            EditRequestedDeliveryDate = detail.RequestedDeliveryDate ?? string.Empty;
                            EditStatus = detail.Status ?? "draft";

                            // Load lines
                            OrderLines.Clear();
                            if (detail.Lines != null)
                                foreach (var line in detail.Lines)
                                    OrderLines.Add(line);

                            // Load batches
                            OrderBatches.Clear();
                            if (detail.Batches != null)
                                foreach (var batch in detail.Batches)
                                    OrderBatches.Add(batch);

                            OnPropertyChanged(nameof(HasLines));
                            OnPropertyChanged(nameof(HasBatches));
                            OnPropertyChanged(nameof(TotalLineQuantity));

                            // Load dropdowns
                            await LoadSupplierOptionsAsync();
                            await LoadProductOptionsAsync();

                            EditSelectedSupplier = SupplierOptions.FirstOrDefault(s => s.Id == detail.SupplierId);
                            EditSelectedProduct = ProductOptions.FirstOrDefault(p => p.Id == detail.ProductId);

                            // Load variants for order lines
                            if (detail.ProductId > 0)
                                await LoadVariantsAsync(detail.ProductId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] OpenEditDrawer error: {ex.Message}");
            }

            SetDrawerMode(PoDrawerMode.Edit);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(PoDrawerMode.None);
        }

        private async Task SaveOrderAsync()
        {
            IsSaving = true;
            StatusMessage = Strings.Msg_Saving;

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
                    ["po_number"] = EditPoNumber.Trim(),
                    ["supplier_id"] = EditSelectedSupplier?.Id,
                    ["product_id"] = EditSelectedProduct?.Id,
                    ["quantity"] = int.TryParse(EditQuantity, out var qty) ? qty : null,
                    ["requested_delivery_date"] = NullIfEmpty(EditRequestedDeliveryDate)
                };

                string? result;
                if (_drawerMode == PoDrawerMode.New)
                {
                    result = await _apiClient.PostWithTenantKeyAsync("/api/purchase-orders", payload, tenantKey);
                }
                else
                {
                    result = await _apiClient.PutWithTenantKeyAsync($"/api/purchase-orders/{_editOrderId}", payload, tenantKey);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadOrdersAsync();
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var err))
                    {
                        StatusMessage = $"Fel: {err.GetString()}";
                        return;
                    }
                }
                StatusMessage = Strings.Msg_NoResponse;
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

        private async Task DeleteOrderAsync(PurchaseOrderSummary? order)
        {
            if (order == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            var confirmText = Strings.Confirm_Delete;
            var result = MessageBox.Show(
                $"{confirmText}\n\n{order.PoNumber}",
                Strings.Action_Delete,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/purchase-orders/{order.Id}", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadOrdersAsync();
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

        private async Task SendOrderAsync(PurchaseOrderSummary? order)
        {
            if (order == null || order.Status != "draft") return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync(
                    $"/api/purchase-orders/{order.Id}/send",
                    new Dictionary<string, object?>(),
                    tenantKey);

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadOrdersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Send error: {ex.Message}");
            }
        }

        private async Task FulfillOrderAsync(PurchaseOrderSummary? order)
        {
            if (order == null || order.Status != "accepted") return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync(
                    $"/api/purchase-orders/{order.Id}/fulfill",
                    new Dictionary<string, object?>(),
                    tenantKey);

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadOrdersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Fulfill error: {ex.Message}");
            }
        }

        private async Task CancelOrderAsync(PurchaseOrderSummary? order)
        {
            if (order == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            var confirmText = Strings.Confirm_CancelOrder;
            var result = MessageBox.Show(
                $"{confirmText}\n\n{order.PoNumber}",
                Strings.Action_CancelOrder,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync(
                    $"/api/purchase-orders/{order.Id}/cancel",
                    new Dictionary<string, object?>(),
                    tenantKey);

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadOrdersAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Cancel error: {ex.Message}");
            }
        }

        private async Task AddLineAsync()
        {
            if (_editOrderId == null || SelectedVariant == null) return;
            if (!int.TryParse(NewLineQuantity, out var qty) || qty < 1) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["product_variant_id"] = SelectedVariant.Id,
                    ["quantity"] = qty
                };

                var json = await _apiClient.PostWithTenantKeyAsync($"/api/purchase-orders/{_editOrderId}/lines", payload, tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        SelectedVariant = null;
                        NewLineQuantity = "1";
                        await ReloadOrderDetailAsync();
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

        private async Task RemoveLineAsync(PurchaseOrderLine? line)
        {
            if (line == null) return;

            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/purchase-order-lines/{line.Id}", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadOrderDetailAsync();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] RemoveLine error: {ex.Message}");
            }
        }

        // Data loading

        private async Task LoadOrdersAsync()
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/purchase-orders", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<PurchaseOrderSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allOrders = items;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadOrdersAsync()
        {
            _allOrders.Clear();
            PurchaseOrders.Clear();
            await LoadOrdersAsync();
        }

        private async Task ReloadOrderDetailAsync()
        {
            if (_editOrderId == null) return;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/purchase-orders/{_editOrderId}", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<PurchaseOrderDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            OrderLines.Clear();
                            if (detail.Lines != null)
                                foreach (var line in detail.Lines)
                                    OrderLines.Add(line);

                            OrderBatches.Clear();
                            if (detail.Batches != null)
                                foreach (var batch in detail.Batches)
                                    OrderBatches.Add(batch);

                            OnPropertyChanged(nameof(HasLines));
                            OnPropertyChanged(nameof(HasBatches));
                            OnPropertyChanged(nameof(TotalLineQuantity));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] ReloadDetail error: {ex.Message}");
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
                            foreach (var item in items)
                                SupplierOptions.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Load suppliers error: {ex.Message}");
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
                            foreach (var item in items)
                                ProductOptions.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Load products error: {ex.Message}");
            }
        }

        private async Task LoadVariantsAsync(int productId)
        {
            var tenantKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(tenantKey)) return;

            try
            {
                AvailableVariants.Clear();
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{productId}/variants", tenantKey);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<VariantInfo>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                AvailableVariants.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandPOs] Load variants error: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            PurchaseOrders.Clear();
            var search = _searchText.Trim();
            IEnumerable<PurchaseOrderSummary> filtered = _allOrders;

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(po =>
                    (po.PoNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (po.SupplierName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (po.ProductName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected && o.Value != null).Select(o => o.Value!).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(po => po.Status != null && activeFilters.Contains(po.Status));

            foreach (var po in filtered)
                PurchaseOrders.Add(po);

            OnPropertyChanged(nameof(HasOrders));
        }

        private void OnLanguageChanged()
        {
            var selectedValues = _statusFilterOptions.Where(o => o.IsSelected && o.Value != null).Select(o => o.Value!).ToHashSet();
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged -= OnFilterChanged;
            _statusFilterOptions = PoStatusFilterOption.All;
            foreach (var opt in _statusFilterOptions)
            {
                if (opt.Value != null) opt.IsSelected = selectedValues.Contains(opt.Value);
                opt.PropertyChanged += OnFilterChanged;
            }
            OnPropertyChanged(nameof(StatusFilterOptions));
            OnPropertyChanged(nameof(DrawerTitle));
            var savedOrder = _selectedOrder;
            ApplyFilter();
            if (savedOrder != null)
                SelectedOrder = PurchaseOrders.FirstOrDefault(o => o.Id == savedOrder.Id);
        }

        private static string? NullIfEmpty(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
