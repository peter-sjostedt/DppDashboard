using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using HospitexDPP.Models;
using HospitexDPP.Resources;
using HospitexDPP.Services;

namespace HospitexDPP.ViewModels
{
    public enum MaterialViewDrawerMode { None, MaterialDetail }

    public class BrandSuppliersViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<SupplierMaterialGroup> _allGroups = new();
        private string _searchText = string.Empty;
        private List<CertFilterOption> _certFilterOptions = CertFilterOption.All;
        private MaterialViewDrawerMode _drawerMode = MaterialViewDrawerMode.None;
        private MaterialSummary? _selectedMaterial;
        private string _drawerTitle = string.Empty;

        private string? _materialName;
        private string? _materialType;
        private string? _materialDescription;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? OnDataChanged;

        public BrandSuppliersViewModel()
        {
            _apiClient = App.ApiClient;

            ViewMaterialCommand = new RelayCommand(async p => await OpenMaterialDrawer(p as MaterialSummary));
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());

            foreach (var opt in _certFilterOptions)
                opt.PropertyChanged += OnCertFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadSuppliersAsync();
        }

        public ObservableCollection<SupplierMaterialGroup> GroupedSuppliers { get; } = new();

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

        public List<CertFilterOption> CertFilterOptions
        {
            get => _certFilterOptions;
            private set { _certFilterOptions = value; OnPropertyChanged(); }
        }

        public int TotalCount => _allGroups.Sum(g => g.Materials.Count);

        private void OnCertFilterChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CertFilterOption.IsSelected))
                ApplyFilter();
        }

        /// <summary>Set cert filter programmatically (from dashboard card).</summary>
        public void SetCertFilter(string certStatus)
        {
            foreach (var opt in _certFilterOptions)
                opt.IsSelected = opt.Value == certStatus;
        }

        public bool IsDrawerOpen => _drawerMode != MaterialViewDrawerMode.None;
        public bool HasSuppliers => GroupedSuppliers.Count > 0;

        public string DrawerTitle
        {
            get => _drawerTitle;
            private set { _drawerTitle = value; OnPropertyChanged(); }
        }

        public string? MaterialName
        {
            get => _materialName;
            private set { _materialName = value; OnPropertyChanged(); }
        }

        public string? MaterialType
        {
            get => _materialType;
            private set { _materialType = value; OnPropertyChanged(); }
        }

        public string? MaterialDescription
        {
            get => _materialDescription;
            private set { _materialDescription = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MaterialComposition> MaterialCompositions { get; } = new();
        public ObservableCollection<MaterialCertification> MaterialCertifications { get; } = new();
        public ObservableCollection<MaterialSupplyChainStep> MaterialSupplyChain { get; } = new();

        // Commands
        public ICommand ViewMaterialCommand { get; }
        public ICommand CancelDrawerCommand { get; }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/suppliers", App.Session!.BrandKey!);
                if (json == null) return;

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var dataArray)) return;

                var groups = new List<SupplierMaterialGroup>();

                foreach (var supplierEl in dataArray.EnumerateArray())
                {
                    var supplierId = supplierEl.GetProperty("id").GetInt32();
                    var supplierName = supplierEl.GetProperty("supplier_name").GetString() ?? string.Empty;
                    string? supplierLocation = null;
                    if (supplierEl.TryGetProperty("supplier_location", out var locProp) && locProp.ValueKind == JsonValueKind.String)
                        supplierLocation = locProp.GetString();

                    // Load materials for this supplier
                    var materials = new List<MaterialSummary>();
                    var matJson = await _apiClient.GetWithTenantKeyAsync($"/api/suppliers/{supplierId}/materials", App.Session!.BrandKey!);
                    if (matJson != null)
                    {
                        using var matDoc = JsonDocument.Parse(matJson);
                        if (matDoc.RootElement.TryGetProperty("data", out var matData))
                        {
                            var items = JsonSerializer.Deserialize<List<MaterialSummary>>(matData.GetRawText(), JsonOptions);
                            if (items != null)
                                materials = items;
                        }
                    }

                    var group = new SupplierMaterialGroup
                    {
                        SupplierId = supplierId,
                        SupplierName = supplierName,
                        SupplierLocation = supplierLocation,
                        Materials = new ObservableCollection<MaterialSummary>(materials)
                    };

                    groups.Add(group);
                }

                _allGroups = groups;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandSuppliers] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();

            // Load cert statuses in background
            await LoadCertStatusesAsync();
        }

        private async Task LoadCertStatusesAsync()
        {
            var brandKey = App.Session?.BrandKey;
            if (string.IsNullOrEmpty(brandKey)) return;

            var allMaterials = _allGroups.SelectMany(g => g.Materials).ToList();
            if (allMaterials.Count == 0) return;

            var threshold = DateTime.Now.AddDays(30);
            var tasks = allMaterials.Select(async mat =>
            {
                try
                {
                    var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{mat.Id}/certifications", brandKey);
                    if (json == null) { mat.CertStatus = "none"; return; }
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("data", out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                    { mat.CertStatus = "none"; return; }

                    var hasExpired = false;
                    var hasExpiring = false;
                    foreach (var cert in arr.EnumerateArray())
                    {
                        if (cert.TryGetProperty("valid_until", out var vu) && vu.ValueKind == JsonValueKind.String)
                        {
                            if (DateTime.TryParse(vu.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                            {
                                if (date < DateTime.Now) hasExpired = true;
                                else if (date <= threshold) hasExpiring = true;
                            }
                        }
                    }

                    mat.CertStatus = hasExpired ? "expired" : hasExpiring ? "expiring" : "ok";
                }
                catch { mat.CertStatus = "none"; }
            });

            await Task.WhenAll(tasks);
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            GroupedSuppliers.Clear();

            var filter = _searchText.Trim();
            var activeCertFilters = _certFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();

            foreach (var group in _allGroups)
            {
                var matchingMaterials = group.Materials.AsEnumerable();

                if (!string.IsNullOrEmpty(filter))
                {
                    var nameMatch = group.SupplierName.Contains(filter, StringComparison.OrdinalIgnoreCase);
                    if (!nameMatch)
                        matchingMaterials = matchingMaterials.Where(m =>
                            m.MaterialName.Contains(filter, StringComparison.OrdinalIgnoreCase));
                }

                if (activeCertFilters.Count > 0)
                    matchingMaterials = matchingMaterials.Where(m => activeCertFilters.Contains(m.CertStatus));

                var filteredList = matchingMaterials.ToList();
                if (filteredList.Count > 0 || (string.IsNullOrEmpty(filter) && activeCertFilters.Count == 0))
                {
                    var filteredGroup = new SupplierMaterialGroup
                    {
                        SupplierId = group.SupplierId,
                        SupplierName = group.SupplierName,
                        SupplierLocation = group.SupplierLocation,
                        Materials = activeCertFilters.Count > 0 || !string.IsNullOrEmpty(filter)
                            ? new ObservableCollection<MaterialSummary>(filteredList)
                            : group.Materials
                    };
                    GroupedSuppliers.Add(filteredGroup);
                }
            }

            OnPropertyChanged(nameof(HasSuppliers));
        }

        private async Task OpenMaterialDrawer(MaterialSummary? material)
        {
            if (material == null) return;

            _selectedMaterial = material;
            MaterialName = material.MaterialName;
            MaterialType = material.MaterialType;
            MaterialDescription = material.Description;

            // Load compositions
            MaterialCompositions.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/compositions", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialComposition>>(data.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items) MaterialCompositions.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandSuppliers] Compositions load error: {ex.Message}");
            }

            // Load certifications
            MaterialCertifications.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/certifications", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialCertification>>(data.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items) MaterialCertifications.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandSuppliers] Certifications load error: {ex.Message}");
            }

            // Load supply chain
            MaterialSupplyChain.Clear();
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/materials/{material.Id}/supply-chain", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var items = JsonSerializer.Deserialize<List<MaterialSupplyChainStep>>(data.GetRawText(), JsonOptions);
                        if (items != null)
                            foreach (var item in items) MaterialSupplyChain.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandSuppliers] Supply chain load error: {ex.Message}");
            }

            DrawerTitle = material.MaterialName;
            SetDrawerMode(MaterialViewDrawerMode.MaterialDetail);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(MaterialViewDrawerMode.None);
        }

        private void SetDrawerMode(MaterialViewDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private void OnLanguageChanged()
        {
            var selectedCertValues = _certFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            foreach (var opt in _certFilterOptions)
                opt.PropertyChanged -= OnCertFilterChanged;
            _certFilterOptions = CertFilterOption.All;
            foreach (var opt in _certFilterOptions)
            {
                opt.IsSelected = selectedCertValues.Contains(opt.Value);
                opt.PropertyChanged += OnCertFilterChanged;
            }
            OnPropertyChanged(nameof(CertFilterOptions));

            OnPropertyChanged(nameof(DrawerTitle));
            // Refresh compositions so RecycledDisplay re-evaluates with new culture
            var comps = MaterialCompositions.ToList();
            MaterialCompositions.Clear();
            foreach (var c in comps) MaterialCompositions.Add(c);
            ApplyFilter();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
