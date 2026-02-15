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
    public class BrandDashboardViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly ApiClient _apiClient;
        private readonly Action<int> _navigateToTab;
        private readonly BrandProductsViewModel _productsTab;
        private readonly BrandSuppliersViewModel _suppliersTab;
        private readonly BrandPurchaseOrdersViewModel _purchaseOrdersTab;
        private readonly BrandBatchesViewModel _batchesTab;
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
            App.Session?.BrandName ?? "Brand");

        public string NoActionsText =>
            Strings.ResourceManager.GetString("Dashboard_NoActions", Strings.Culture) ?? "Everything under control";

        public BrandDashboardViewModel(Action<int> navigateToTab,
            BrandProductsViewModel productsTab,
            BrandSuppliersViewModel suppliersTab,
            BrandPurchaseOrdersViewModel purchaseOrdersTab,
            BrandBatchesViewModel batchesTab)
        {
            _apiClient = App.ApiClient;
            _navigateToTab = navigateToTab;
            _productsTab = productsTab;
            _suppliersTab = suppliersTab;
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

            var brandKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(brandKey)) { IsLoading = false; return; }

            try
            {
                // Load simple list data in parallel
                var poTask = LoadListAsync<PurchaseOrderSummary>("/api/purchase-orders", brandKey);
                var batchTask = LoadListAsync<BatchSummary>("/api/batches", brandKey);
                var productTask = LoadListAsync<ProductSummary>("/api/products", brandKey);

                await Task.WhenAll(poTask, batchTask, productTask);

                var pos = poTask.Result;
                var batches = batchTask.Result;
                var products = productTask.Result;

                // Card 1: Rejected/cancelled orders
                var rejectedCount = pos.Count(p => p.Status == "cancelled");
                if (rejectedCount > 0)
                    AddCard("\U0001F4CB", "Dashboard_RejectedOrders", "Dashboard_RejectedOrders_Desc", rejectedCount, () =>
                    {
                        _purchaseOrdersTab.SetStatusFilter("cancelled");
                        _navigateToTab(2);
                    });

                // Card 2: Completed batches to review
                var completedBatchCount = batches.Count(b => b.Status == "completed");
                if (completedBatchCount > 0)
                    AddCard("\U0001F4E6", "Dashboard_CompletedBatches", "Dashboard_CompletedBatches_Desc", completedBatchCount, () =>
                    {
                        _batchesTab.SetStatusFilter("completed");
                        _navigateToTab(3);
                    });

                // Card 3 & 4: Product completeness (parallel detail loading)
                var incompleteCount = 0;
                var completeCount = 0;

                if (products.Count > 0)
                {
                    var detailTasks = products.Select(async p =>
                    {
                        try
                        {
                            var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{p.Id}", brandKey);
                            if (json == null) return (false, false);
                            using var doc = JsonDocument.Parse(json);
                            if (!doc.RootElement.TryGetProperty("data", out var data)) return (false, false);

                            var hasCare = data.TryGetProperty("care_information", out var care) && care.ValueKind != JsonValueKind.Null;
                            var hasCompliance = data.TryGetProperty("compliance_information", out var comp) && comp.ValueKind != JsonValueKind.Null;
                            var hasComponents = data.TryGetProperty("components", out var comps) && comps.ValueKind == JsonValueKind.Array && comps.GetArrayLength() > 0;

                            // Check variants
                            var hasVariants = false;
                            var varJson = await _apiClient.GetWithTenantKeyAsync($"/api/products/{p.Id}/variants", brandKey);
                            if (varJson != null)
                            {
                                using var varDoc = JsonDocument.Parse(varJson);
                                if (varDoc.RootElement.TryGetProperty("data", out var varArr) && varArr.ValueKind == JsonValueKind.Array)
                                    hasVariants = varArr.GetArrayLength() > 0;
                            }

                            var isComplete = hasCare && hasCompliance && hasComponents && hasVariants;
                            return (isComplete, true);
                        }
                        catch { return (false, false); }
                    }).ToList();

                    var results = await Task.WhenAll(detailTasks);
                    foreach (var (isComplete, loaded) in results)
                    {
                        if (!loaded) continue;
                        if (isComplete) completeCount++;
                        else incompleteCount++;
                    }
                }

                if (incompleteCount > 0)
                    AddCard("\U0001F4DD", "Dashboard_IncompleteProducts", "Dashboard_IncompleteProducts_Desc", incompleteCount, () =>
                    {
                        _productsTab.SetCompletenessFilter("incomplete");
                        _navigateToTab(1);
                    });

                if (completeCount > 0)
                    AddCard("\U0001F680", "Dashboard_ReadyForExport", "Dashboard_ReadyForExport_Desc", completeCount, () =>
                    {
                        _productsTab.SetCompletenessFilter("complete");
                        _navigateToTab(1);
                    });

                // Card 5 & 6: Expired / Expiring certifications
                var (certExpiring, certExpired) = await CountCertificationsAsync(brandKey);

                if (certExpired > 0)
                    AddCard("\u274C", "Dashboard_ExpiredCerts", "Dashboard_ExpiredCerts_Desc", certExpired, () =>
                    {
                        _suppliersTab.SetCertFilter("expired");
                        _navigateToTab(4);
                    });

                if (certExpiring > 0)
                    AddCard("\u26A0\uFE0F", "Dashboard_ExpiringCerts", "Dashboard_ExpiringCerts_Desc", certExpiring, () =>
                    {
                        _suppliersTab.SetCertFilter("expiring");
                        _navigateToTab(4);
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandDashboard] Load error: {ex.Message}");
            }

            IsLoading = false;
            OnPropertyChanged(nameof(ShowNoActions));
        }

        private async Task<(int expiring, int expired)> CountCertificationsAsync(string brandKey)
        {
            try
            {
                // Load suppliers
                var suppJson = await _apiClient.GetWithTenantKeyAsync("/api/brand-suppliers", brandKey);
                if (suppJson == null) return (0, 0);

                using var suppDoc = JsonDocument.Parse(suppJson);
                if (!suppDoc.RootElement.TryGetProperty("data", out var suppArr) || suppArr.ValueKind != JsonValueKind.Array)
                    return (0, 0);

                var supplierIds = new List<int>();
                foreach (var s in suppArr.EnumerateArray())
                {
                    if (s.TryGetProperty("supplier_id", out var sid) && sid.TryGetInt32(out var id))
                        supplierIds.Add(id);
                }

                // Load materials per supplier
                var matTasks = supplierIds.Select(async sid =>
                {
                    var json = await _apiClient.GetWithTenantKeyAsync($"/api/suppliers/{sid}/materials", brandKey);
                    if (json == null) return new List<int>();
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("data", out var arr) || arr.ValueKind != JsonValueKind.Array)
                        return new List<int>();
                    return arr.EnumerateArray()
                        .Where(m => m.TryGetProperty("id", out _))
                        .Select(m => m.GetProperty("id").GetInt32())
                        .ToList();
                });

                var matResults = await Task.WhenAll(matTasks);
                var materialIds = matResults.SelectMany(x => x).Distinct().ToList();

                // Load certifications per material
                var now = DateTime.Now;
                var threshold = now.AddDays(30);

                var certTasks = materialIds.Select(async mid =>
                {
                    try
                    {
                        var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{mid}/certifications", brandKey);
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
                return (certResults.Sum(r => r.Item1), certResults.Sum(r => r.Item2));
            }
            catch { return (0, 0); }
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
                Debug.WriteLine($"[BrandDashboard] LoadList error for {endpoint}: {ex.Message}");
            }
            return [];
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
