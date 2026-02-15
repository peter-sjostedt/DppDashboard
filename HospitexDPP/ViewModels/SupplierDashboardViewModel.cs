using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public class SupplierDashboardViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly ApiClient _apiClient;
        private readonly Action<int> _navigateToTab;
        private readonly SupplierMaterialsViewModel _materialsTab;
        private readonly SupplierPurchaseOrdersViewModel _purchaseOrdersTab;
        private readonly SupplierBatchesViewModel _batchesTab;
        private bool _isLoading = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ActionCard> Cards { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowNoActions)); }
        }

        public bool ShowNoActions => !IsLoading && Cards.Count == 0;

        public string WelcomeText => string.Format(
            Strings.ResourceManager.GetString("Dashboard_Welcome", Strings.Culture) ?? "Welcome, {0}",
            App.Session?.SupplierName ?? "Supplier");

        public string NoActionsText =>
            Strings.ResourceManager.GetString("Dashboard_NoActions", Strings.Culture) ?? "Everything under control";

        public SupplierDashboardViewModel(Action<int> navigateToTab,
            SupplierMaterialsViewModel materialsTab,
            SupplierPurchaseOrdersViewModel purchaseOrdersTab,
            SupplierBatchesViewModel batchesTab)
        {
            _apiClient = App.ApiClient;
            _navigateToTab = navigateToTab;
            _materialsTab = materialsTab;
            _purchaseOrdersTab = purchaseOrdersTab;
            _batchesTab = batchesTab;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadDashboardAsync();
        }

        private void OnLanguageChanged()
        {
            OnPropertyChanged(nameof(WelcomeText));
            OnPropertyChanged(nameof(NoActionsText));
            _ = LoadDashboardAsync();
        }

        private async Task LoadDashboardAsync()
        {
            IsLoading = true;
            Cards.Clear();

            var supplierKey = App.Session?.SupplierKey;
            if (string.IsNullOrEmpty(supplierKey)) { IsLoading = false; return; }

            try
            {
                // Load simple list data in parallel
                var poTask = LoadListAsync<PurchaseOrderSummary>("/api/purchase-orders", supplierKey);
                var batchTask = LoadListAsync<BatchSummary>("/api/batches", supplierKey);
                var materialTask = LoadListAsync<MaterialSummary>("/api/materials", supplierKey);

                await Task.WhenAll(poTask, batchTask, materialTask);

                var pos = poTask.Result;
                var batches = batchTask.Result;
                var materials = materialTask.Result;

                // Card 1: Pending orders (sent, awaiting supplier action)
                var pendingCount = pos.Count(p => p.Status == "sent");
                if (pendingCount > 0)
                    AddCard("\U0001F4CB", "Dashboard_PendingOrders", "Dashboard_PendingOrders_Desc", pendingCount, () =>
                    {
                        _purchaseOrdersTab.SetStatusFilter("sent");
                        _navigateToTab(2);
                    });

                // Card 2: Batches without materials (in_production, need to check per-batch)
                var inProdBatches = batches.Where(b => b.Status == "in_production").ToList();

                if (inProdBatches.Count > 0)
                {
                    var matCheckTasks = inProdBatches.Select(async batch =>
                    {
                        try
                        {
                            var json = await _apiClient.GetWithTenantKeyAsync($"/api/batches/{batch.Id}/materials", supplierKey);
                            if (json == null) return true; // assume no materials
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("data", out var arr) && arr.ValueKind == JsonValueKind.Array)
                                return arr.GetArrayLength() == 0;
                            return true;
                        }
                        catch { return true; }
                    });

                    var matResults = await Task.WhenAll(matCheckTasks);
                    var noMaterialCount = matResults.Count(noMat => noMat);
                    if (noMaterialCount > 0)
                        AddCard("\U0001F9F5", "Dashboard_BatchesNoMaterials", "Dashboard_BatchesNoMaterials_Desc", noMaterialCount, () =>
                        {
                            _batchesTab.SetStatusFilter("in_production");
                            _navigateToTab(3);
                        });
                }

                // Card 3: Batches without items
                var noItemCount = inProdBatches.Count(b => (b.ItemCount ?? 0) == 0);
                if (noItemCount > 0)
                    AddCard("\U0001F4E6", "Dashboard_BatchesNoItems", "Dashboard_BatchesNoItems_Desc", noItemCount, () =>
                    {
                        _batchesTab.SetStatusFilter("in_production");
                        _navigateToTab(3);
                    });

                // Card 4: Incomplete materials (no compositions)
                if (materials.Count > 0)
                {
                    var compTasks = materials.Select(async mat =>
                    {
                        try
                        {
                            var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{mat.Id}/compositions", supplierKey);
                            if (json == null) return true;
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("data", out var arr) && arr.ValueKind == JsonValueKind.Array)
                                return arr.GetArrayLength() == 0;
                            return true;
                        }
                        catch { return true; }
                    });

                    var compResults = await Task.WhenAll(compTasks);
                    var incompleteMatCount = compResults.Count(noComp => noComp);
                    if (incompleteMatCount > 0)
                        AddCard("\U0001F4DD", "Dashboard_IncompleteMaterials", "Dashboard_IncompleteMaterials_Desc", incompleteMatCount, 1);
                }

                // Card 5 & 6: Expiring / Expired certifications
                if (materials.Count > 0)
                {
                    var now = DateTime.Now;
                    var threshold = now.AddDays(30);
                    var certTasks = materials.Select(async mat =>
                    {
                        try
                        {
                            var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{mat.Id}/certifications", supplierKey);
                            if (json == null) return (0, 0);
                            using var doc = JsonDocument.Parse(json);
                            if (!doc.RootElement.TryGetProperty("data", out var arr) || arr.ValueKind != JsonValueKind.Array)
                                return (0, 0);
                            var expiring = 0;
                            var expired = 0;
                            foreach (var cert in arr.EnumerateArray())
                            {
                                if (cert.TryGetProperty("valid_until", out var vu) && vu.ValueKind == JsonValueKind.String)
                                {
                                    if (DateTime.TryParse(vu.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                                    {
                                        if (date < now) expired++;
                                        else if (date <= threshold) expiring++;
                                    }
                                }
                            }
                            return (expiring, expired);
                        }
                        catch { return (0, 0); }
                    });

                    var certResults = await Task.WhenAll(certTasks);
                    var expiringCount = certResults.Sum(r => r.Item1);
                    var expiredCount = certResults.Sum(r => r.Item2);

                    if (expiredCount > 0)
                        AddCard("\u274C", "Dashboard_ExpiredCerts", "Dashboard_ExpiredCerts_Desc", expiredCount, () =>
                        {
                            _materialsTab.SetCertFilter("expired");
                            _navigateToTab(1);
                        });

                    if (expiringCount > 0)
                        AddCard("\u26A0\uFE0F", "Dashboard_ExpiringCerts", "Dashboard_ExpiringCerts_Desc", expiringCount, () =>
                        {
                            _materialsTab.SetCertFilter("expiring");
                            _navigateToTab(1);
                        });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierDashboard] Load error: {ex.Message}");
            }

            IsLoading = false;
            OnPropertyChanged(nameof(ShowNoActions));
        }

        private void AddCard(string icon, string titleKey, string descKey, int count, int targetTab)
        {
            AddCard(icon, titleKey, descKey, count, () => _navigateToTab(targetTab));
        }

        private void AddCard(string icon, string titleKey, string descKey, int count, Action action)
        {
            var title = Strings.ResourceManager.GetString(titleKey, Strings.Culture) ?? titleKey;
            var descTemplate = Strings.ResourceManager.GetString(descKey, Strings.Culture) ?? $"{{0}} items";
            var desc = string.Format(descTemplate, count);

            Cards.Add(new ActionCard
            {
                Icon = icon,
                Title = title,
                Count = count,
                Description = desc,
                NavigateCommand = new RelayCommand(_ => action())
            });
        }

        private async Task<List<T>> LoadListAsync<T>(string endpoint, string tenantKey)
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync(endpoint, tenantKey);
                if (json == null) return [];

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataArr))
                {
                    return JsonSerializer.Deserialize<List<T>>(dataArr.GetRawText(), JsonOptions) ?? [];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierDashboard] LoadList error for {endpoint}: {ex.Message}");
            }
            return [];
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
