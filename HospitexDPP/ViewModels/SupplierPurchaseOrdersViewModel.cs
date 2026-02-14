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
    public class PoStatusFilterOption : INotifyPropertyChanged
    {
        public string Label { get; set; } = "";
        public string? Value { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
        }

        public string DisplayName => Label;
        public override string ToString() => Label;

        public static List<PoStatusFilterOption> All => new()
        {
            new() { Label = Application.Current.TryFindResource("Status_Draft") as string ?? "Utkast", Value = "draft" },
            new() { Label = Application.Current.TryFindResource("Status_Sent") as string ?? "Skickad", Value = "sent" },
            new() { Label = Application.Current.TryFindResource("Status_Accepted") as string ?? "Accepterad", Value = "accepted" },
            new() { Label = Application.Current.TryFindResource("Status_Fulfilled") as string ?? "Levererad", Value = "fulfilled" },
            new() { Label = Application.Current.TryFindResource("Status_Cancelled") as string ?? "Avbruten", Value = "cancelled" },
        };

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class SupplierPurchaseOrdersViewModel : INotifyPropertyChanged
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
        private PurchaseOrderDetail? _selectedDetail;
        private bool _isDrawerOpen;
        private bool _isLoading;
        private bool _isAccepting;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? OnDataChanged;

        public SupplierPurchaseOrdersViewModel()
        {
            _apiClient = App.ApiClient;
            var saved = SettingsService.LoadFilter("supplier_pos");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    if (opt.Value != null) opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;

            SelectCommand = new RelayCommand(async p => await SelectPurchaseOrderAsync(p as PurchaseOrderSummary));
            AcceptCommand = new RelayCommand(async _ => await AcceptPurchaseOrderAsync(), _ => _selectedDetail?.CanAccept == true && !_isAccepting);
            AcceptDirectCommand = new RelayCommand(async p => await AcceptDirectAsync(p as PurchaseOrderSummary), _ => !_isAccepting);
            CloseDrawerCommand = new RelayCommand(_ => CloseDrawer());
            RefreshCommand = new RelayCommand(async _ => await ReloadAsync());

            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadPurchaseOrdersAsync();
        }

        public ObservableCollection<PurchaseOrderSummary> PurchaseOrders { get; } = new();

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
                SettingsService.SaveFilter("supplier_pos", string.Join(",", sel));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public PurchaseOrderDetail? SelectedDetail
        {
            get => _selectedDetail;
            private set
            {
                _selectedDetail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasBatches));
                OnPropertyChanged(nameof(HasLines));
                OnPropertyChanged(nameof(TotalLineQuantity));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            private set { _isDrawerOpen = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool HasOrders => PurchaseOrders.Count > 0;
        public bool HasBatches => SelectedDetail?.Batches != null && SelectedDetail.Batches.Count > 0;
        public bool HasLines => SelectedDetail?.Lines != null && SelectedDetail.Lines.Count > 0;
        public int TotalLineQuantity => SelectedDetail?.Lines?.Sum(l => l.Quantity) ?? 0;
        public int TotalCount => _allOrders.Count;

        public ICommand SelectCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand AcceptDirectCommand { get; }
        public ICommand CloseDrawerCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadPurchaseOrdersAsync()
        {
            IsLoading = true;
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/purchase-orders", App.Session!.SupplierKey!);
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
                Debug.WriteLine($"[PurchaseOrders] Load error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task SelectPurchaseOrderAsync(PurchaseOrderSummary? po)
        {
            if (po == null) return;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/purchase-orders/{po.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<PurchaseOrderDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            SelectedDetail = detail;
                            IsDrawerOpen = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PurchaseOrders] SelectPO error: {ex.Message}");
            }
        }

        private async Task AcceptPurchaseOrderAsync()
        {
            if (_selectedDetail == null) return;

            _isAccepting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync(
                    $"/api/purchase-orders/{_selectedDetail.Id}/accept",
                    new Dictionary<string, object?>(),
                    App.Session!.SupplierKey!);

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadAsync();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PurchaseOrders] Accept error: {ex.Message}");
            }
            finally
            {
                _isAccepting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task AcceptDirectAsync(PurchaseOrderSummary? po)
        {
            if (po == null || po.Status != "sent") return;

            _isAccepting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync(
                    $"/api/purchase-orders/{po.Id}/accept",
                    new Dictionary<string, object?>(),
                    App.Session!.SupplierKey!);

                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadAsync();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PurchaseOrders] AcceptDirect error: {ex.Message}");
            }
            finally
            {
                _isAccepting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void CloseDrawer()
        {
            IsDrawerOpen = false;
            SelectedDetail = null;
        }

        private async Task ReloadAsync()
        {
            _allOrders.Clear();
            PurchaseOrders.Clear();
            OnPropertyChanged(nameof(HasOrders));
            await LoadPurchaseOrdersAsync();
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
                    (po.BrandName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
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
            ApplyFilter();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
