using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using DppDashboard.Models;
using DppDashboard.Services;

namespace DppDashboard.ViewModels
{
    public class RelationEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("brand_id")]
        public int BrandId { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; }

        // Enriched from suppliers list
        public string? SupplierName { get; set; }
        public string? SupplierLocation { get; set; }
    }

    public class RelationsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private readonly List<SupplierDetail> _allSuppliers = new();
        private BrandSummary? _selectedBrand;
        private RelationEntry? _selectedRelation;
        private string _statusText = "Brands";
        private string _linkedSuppliersHeader = "Kopplade suppliers";

        public event PropertyChangedEventHandler? PropertyChanged;

        public RelationsViewModel()
        {
            _apiClient = App.ApiClient;

            LinkSupplierCommand = new RelayCommand(_ => MessageBox.Show("Koppla supplier - kommer snart", "Koppla supplier"), _ => _selectedBrand != null);
            UnlinkSupplierCommand = new RelayCommand(_ => MessageBox.Show($"Ta bort koppling till {_selectedRelation?.SupplierName}?", "Ta bort koppling", MessageBoxButton.YesNo), _ => _selectedRelation != null);

            _ = LoadDataAsync();
        }

        public ObservableCollection<BrandSummary> Brands { get; } = new();
        public ObservableCollection<RelationEntry> AllRelations { get; } = new();
        public ObservableCollection<RelationEntry> LinkedSuppliers { get; } = new();

        public ICommand LinkSupplierCommand { get; }
        public ICommand UnlinkSupplierCommand { get; }

        public BrandSummary? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                _selectedBrand = value;
                OnPropertyChanged();
                FilterRelations();
            }
        }

        public RelationEntry? SelectedRelation
        {
            get => _selectedRelation;
            set { _selectedRelation = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string LinkedSuppliersHeader
        {
            get => _linkedSuppliersHeader;
            set { _linkedSuppliersHeader = value; OnPropertyChanged(); }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load brands
                var brandsJson = await _apiClient.GetRawAsync("/api/admin/brands");
                Debug.WriteLine($"[Relations] GET /api/admin/brands => {brandsJson?.Substring(0, Math.Min(brandsJson.Length, 500))}");
                if (brandsJson != null)
                {
                    using var doc = JsonDocument.Parse(brandsJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<BrandSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items)
                                Brands.Add(item);
                    }
                }
                Debug.WriteLine($"[Relations] Loaded {Brands.Count} brands");

                // Load suppliers
                var suppliersJson = await _apiClient.GetRawAsync("/api/admin/suppliers");
                Debug.WriteLine($"[Relations] GET /api/admin/suppliers => {suppliersJson?.Substring(0, Math.Min(suppliersJson.Length, 500))}");
                if (suppliersJson != null)
                {
                    using var doc = JsonDocument.Parse(suppliersJson);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<SupplierDetail>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allSuppliers.AddRange(items);
                    }
                }
                Debug.WriteLine($"[Relations] Loaded {_allSuppliers.Count} suppliers");

                // Load relations
                var relationsJson = await _apiClient.GetRawAsync("/api/admin/relations");
                Debug.WriteLine($"[Relations] GET /api/admin/relations => {relationsJson?.Substring(0, Math.Min(relationsJson.Length, 500))}");
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
                                // Enrich with supplier name and location
                                var supplier = _allSuppliers.FirstOrDefault(s => s.Id == item.SupplierId);
                                item.SupplierName = supplier?.SupplierName ?? $"(Supplier #{item.SupplierId})";
                                item.SupplierLocation = supplier?.SupplierLocation;
                                AllRelations.Add(item);
                                Debug.WriteLine($"[Relations]   Relation id={item.Id} brand_id={item.BrandId} supplier_id={item.SupplierId} => {item.SupplierName}");
                            }
                        }
                    }
                }

                StatusText = $"Brands ({Brands.Count})";
            }
            catch (Exception ex)
            {
                StatusText = $"Fel: {ex.Message}";
                Debug.WriteLine($"[Relations] ERROR: {ex}");
            }
        }

        private void FilterRelations()
        {
            LinkedSuppliers.Clear();
            if (_selectedBrand == null) return;

            Debug.WriteLine($"[Relations] Filter: brand '{_selectedBrand.BrandName}' id={_selectedBrand.Id}");

            foreach (var rel in AllRelations)
            {
                if (rel.BrandId == _selectedBrand.Id)
                {
                    LinkedSuppliers.Add(rel);
                    Debug.WriteLine($"[Relations]   Match: supplier '{rel.SupplierName}' (supplier_id={rel.SupplierId})");
                }
            }

            LinkedSuppliersHeader = _selectedBrand == null
                ? "Kopplade suppliers"
                : $"Kopplade suppliers ({LinkedSuppliers.Count})";
            Debug.WriteLine($"[Relations] Found {LinkedSuppliers.Count} linked suppliers");
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
