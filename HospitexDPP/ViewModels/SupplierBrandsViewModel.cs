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
    public enum SupplierBrandDrawerMode { None, ProductDetail }

    public class SupplierBrandsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<BrandProductGroup> _allGroups = new();
        private string _searchText = string.Empty;
        private SupplierBrandDrawerMode _drawerMode = SupplierBrandDrawerMode.None;
        private string _drawerTitle = string.Empty;

        // Product detail fields
        private string? _detailProductName;
        private string? _detailArticleNumber;
        private string? _detailCategory;
        private string? _detailDescription;
        private string? _detailGtinType;
        private string? _detailGtin;
        private string? _detailProductGroup;
        private string? _detailTypeItem;
        private string? _detailAgeGroup;
        private string? _detailGender;
        private string? _detailMarketSegment;

        // Care fields
        private string? _detailCareImageUrl;
        private string? _detailCareText;
        private string? _detailSafetyInfo;

        // Compliance fields
        private string? _detailHarmfulSubstances;
        private string? _detailHarmfulSubstancesInfo;
        private string? _detailCertifications;
        private string? _detailCertificationsValidation;
        private string? _detailChemicalComplianceStandard;
        private string? _detailChemicalComplianceValidation;
        private string? _detailChemicalComplianceLink;
        private string? _detailMicrofibers;
        private string? _detailTraceabilityProvider;

        // Circularity fields
        private string? _detailPerformance;
        private string? _detailRecyclability;
        private string? _detailTakeBackInstructions;
        private string? _detailRecyclingInstructions;
        private string? _detailDisassemblyInstructionsSorters;
        private string? _detailDisassemblyInstructionsUser;
        private string? _detailCircularDesignStrategy;
        private string? _detailCircularDesignDescription;
        private string? _detailRepairInstructions;

        // Sustainability fields
        private string? _detailBrandStatement;
        private string? _detailStatementLink;
        private string? _detailEnvironmentalFootprint;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after brands are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public SupplierBrandsViewModel()
        {
            _apiClient = App.ApiClient;

            ViewProductCommand = new RelayCommand(async p => await OpenProductDrawer(p as ProductSummary));
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());

            _ = LoadBrandsAsync();
        }

        public ObservableCollection<BrandProductGroup> GroupedBrands { get; } = new();

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

        public bool IsDrawerOpen => _drawerMode != SupplierBrandDrawerMode.None;
        public bool HasBrands => GroupedBrands.Count > 0;
        public int TotalCount => _allGroups.Count;

        public string DrawerTitle
        {
            get => _drawerTitle;
            private set { _drawerTitle = value; OnPropertyChanged(); }
        }

        // Product detail properties
        public string? DetailProductName
        {
            get => _detailProductName;
            private set { _detailProductName = value; OnPropertyChanged(); }
        }

        public string? DetailArticleNumber
        {
            get => _detailArticleNumber;
            private set { _detailArticleNumber = value; OnPropertyChanged(); }
        }

        public string? DetailCategory
        {
            get => _detailCategory;
            private set { _detailCategory = value; OnPropertyChanged(); }
        }

        public string? DetailDescription
        {
            get => _detailDescription;
            private set { _detailDescription = value; OnPropertyChanged(); }
        }

        public string? DetailGtinType
        {
            get => _detailGtinType;
            private set { _detailGtinType = value; OnPropertyChanged(); }
        }

        public string? DetailGtin
        {
            get => _detailGtin;
            private set { _detailGtin = value; OnPropertyChanged(); }
        }

        public string? DetailProductGroup
        {
            get => _detailProductGroup;
            private set { _detailProductGroup = value; OnPropertyChanged(); }
        }

        public string? DetailTypeItem
        {
            get => _detailTypeItem;
            private set { _detailTypeItem = value; OnPropertyChanged(); }
        }

        public string? DetailAgeGroup
        {
            get => _detailAgeGroup;
            private set { _detailAgeGroup = value; OnPropertyChanged(); }
        }

        public string? DetailGender
        {
            get => _detailGender;
            private set { _detailGender = value; OnPropertyChanged(); }
        }

        public string? DetailMarketSegment
        {
            get => _detailMarketSegment;
            private set { _detailMarketSegment = value; OnPropertyChanged(); }
        }

        // Variants and components
        public ObservableCollection<VariantInfo> DetailVariants { get; } = new();
        public ObservableCollection<ComponentInfo> DetailComponents { get; } = new();

        // Care properties
        public string? DetailCareImageUrl
        {
            get => _detailCareImageUrl;
            private set { _detailCareImageUrl = value; OnPropertyChanged(); }
        }

        public string? DetailCareText
        {
            get => _detailCareText;
            private set { _detailCareText = value; OnPropertyChanged(); }
        }

        public string? DetailSafetyInfo
        {
            get => _detailSafetyInfo;
            private set { _detailSafetyInfo = value; OnPropertyChanged(); }
        }

        // Compliance properties
        public string? DetailHarmfulSubstances
        {
            get => _detailHarmfulSubstances;
            private set { _detailHarmfulSubstances = value; OnPropertyChanged(); }
        }

        public string? DetailHarmfulSubstancesInfo
        {
            get => _detailHarmfulSubstancesInfo;
            private set { _detailHarmfulSubstancesInfo = value; OnPropertyChanged(); }
        }

        public string? DetailCertifications
        {
            get => _detailCertifications;
            private set { _detailCertifications = value; OnPropertyChanged(); }
        }

        public string? DetailCertificationsValidation
        {
            get => _detailCertificationsValidation;
            private set { _detailCertificationsValidation = value; OnPropertyChanged(); }
        }

        public string? DetailChemicalComplianceStandard
        {
            get => _detailChemicalComplianceStandard;
            private set { _detailChemicalComplianceStandard = value; OnPropertyChanged(); }
        }

        public string? DetailChemicalComplianceValidation
        {
            get => _detailChemicalComplianceValidation;
            private set { _detailChemicalComplianceValidation = value; OnPropertyChanged(); }
        }

        public string? DetailChemicalComplianceLink
        {
            get => _detailChemicalComplianceLink;
            private set { _detailChemicalComplianceLink = value; OnPropertyChanged(); }
        }

        public string? DetailMicrofibers
        {
            get => _detailMicrofibers;
            private set { _detailMicrofibers = value; OnPropertyChanged(); }
        }

        public string? DetailTraceabilityProvider
        {
            get => _detailTraceabilityProvider;
            private set { _detailTraceabilityProvider = value; OnPropertyChanged(); }
        }

        // Circularity properties
        public string? DetailPerformance
        {
            get => _detailPerformance;
            private set { _detailPerformance = value; OnPropertyChanged(); }
        }

        public string? DetailRecyclability
        {
            get => _detailRecyclability;
            private set { _detailRecyclability = value; OnPropertyChanged(); }
        }

        public string? DetailTakeBackInstructions
        {
            get => _detailTakeBackInstructions;
            private set { _detailTakeBackInstructions = value; OnPropertyChanged(); }
        }

        public string? DetailRecyclingInstructions
        {
            get => _detailRecyclingInstructions;
            private set { _detailRecyclingInstructions = value; OnPropertyChanged(); }
        }

        public string? DetailDisassemblyInstructionsSorters
        {
            get => _detailDisassemblyInstructionsSorters;
            private set { _detailDisassemblyInstructionsSorters = value; OnPropertyChanged(); }
        }

        public string? DetailDisassemblyInstructionsUser
        {
            get => _detailDisassemblyInstructionsUser;
            private set { _detailDisassemblyInstructionsUser = value; OnPropertyChanged(); }
        }

        public string? DetailCircularDesignStrategy
        {
            get => _detailCircularDesignStrategy;
            private set { _detailCircularDesignStrategy = value; OnPropertyChanged(); }
        }

        public string? DetailCircularDesignDescription
        {
            get => _detailCircularDesignDescription;
            private set { _detailCircularDesignDescription = value; OnPropertyChanged(); }
        }

        public string? DetailRepairInstructions
        {
            get => _detailRepairInstructions;
            private set { _detailRepairInstructions = value; OnPropertyChanged(); }
        }

        // Sustainability properties
        public string? DetailBrandStatement
        {
            get => _detailBrandStatement;
            private set { _detailBrandStatement = value; OnPropertyChanged(); }
        }

        public string? DetailStatementLink
        {
            get => _detailStatementLink;
            private set { _detailStatementLink = value; OnPropertyChanged(); }
        }

        public string? DetailEnvironmentalFootprint
        {
            get => _detailEnvironmentalFootprint;
            private set { _detailEnvironmentalFootprint = value; OnPropertyChanged(); }
        }

        // Section visibility computed properties
        public bool HasVariants => DetailVariants.Count > 0;
        public bool HasComponents => DetailComponents.Count > 0;

        public bool HasCare =>
            !string.IsNullOrEmpty(DetailCareImageUrl) ||
            !string.IsNullOrEmpty(DetailCareText) ||
            !string.IsNullOrEmpty(DetailSafetyInfo);

        public bool HasCompliance =>
            !string.IsNullOrEmpty(DetailHarmfulSubstances) ||
            !string.IsNullOrEmpty(DetailHarmfulSubstancesInfo) ||
            !string.IsNullOrEmpty(DetailCertifications) ||
            !string.IsNullOrEmpty(DetailCertificationsValidation) ||
            !string.IsNullOrEmpty(DetailChemicalComplianceStandard) ||
            !string.IsNullOrEmpty(DetailChemicalComplianceValidation) ||
            !string.IsNullOrEmpty(DetailChemicalComplianceLink) ||
            !string.IsNullOrEmpty(DetailMicrofibers) ||
            !string.IsNullOrEmpty(DetailTraceabilityProvider);

        public bool HasCircularity =>
            !string.IsNullOrEmpty(DetailPerformance) ||
            !string.IsNullOrEmpty(DetailRecyclability) ||
            !string.IsNullOrEmpty(DetailTakeBackInstructions) ||
            !string.IsNullOrEmpty(DetailRecyclingInstructions) ||
            !string.IsNullOrEmpty(DetailDisassemblyInstructionsSorters) ||
            !string.IsNullOrEmpty(DetailDisassemblyInstructionsUser) ||
            !string.IsNullOrEmpty(DetailCircularDesignStrategy) ||
            !string.IsNullOrEmpty(DetailCircularDesignDescription) ||
            !string.IsNullOrEmpty(DetailRepairInstructions);

        public bool HasSustainability =>
            !string.IsNullOrEmpty(DetailBrandStatement) ||
            !string.IsNullOrEmpty(DetailStatementLink) ||
            !string.IsNullOrEmpty(DetailEnvironmentalFootprint);

        // Commands
        public ICommand ViewProductCommand { get; }
        public ICommand CancelDrawerCommand { get; }

        private async Task LoadBrandsAsync()
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/brand-suppliers", App.Session!.SupplierKey!);
                if (json == null) return;

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("data", out var dataArray)) return;

                // Deserialize brand-supplier links and get unique brands
                var links = JsonSerializer.Deserialize<List<BrandSupplierLink>>(dataArray.GetRawText(), JsonOptions);
                if (links == null) return;

                var seenBrandIds = new HashSet<int>();
                var uniqueLinks = new List<BrandSupplierLink>();
                foreach (var link in links)
                {
                    if (seenBrandIds.Add(link.BrandId))
                        uniqueLinks.Add(link);
                }

                var groups = new List<BrandProductGroup>();

                foreach (var link in uniqueLinks)
                {
                    // Load products for this brand
                    var products = new List<ProductSummary>();
                    try
                    {
                        var prodJson = await _apiClient.GetWithTenantKeyAsync($"/api/brands/{link.BrandId}/products", App.Session!.SupplierKey!);
                        if (prodJson != null)
                        {
                            using var prodDoc = JsonDocument.Parse(prodJson);
                            if (prodDoc.RootElement.TryGetProperty("data", out var prodData))
                            {
                                var items = JsonSerializer.Deserialize<List<ProductSummary>>(prodData.GetRawText(), JsonOptions);
                                if (items != null)
                                    products = items;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SupplierBrands] Load products for brand {link.BrandId} error: {ex.Message}");
                    }

                    // Load variant counts in parallel
                    var variantTasks = products.Select(async p =>
                    {
                        try
                        {
                            var varJson = await _apiClient.GetWithTenantKeyAsync(
                                $"/api/products/{p.Id}/variants", App.Session!.SupplierKey!);
                            if (varJson != null)
                            {
                                using var varDoc = JsonDocument.Parse(varJson);
                                if (varDoc.RootElement.TryGetProperty("data", out var varArr)
                                    && varArr.ValueKind == JsonValueKind.Array)
                                    p.VariantCount = varArr.GetArrayLength();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[SupplierBrands] Load variants for product {p.Id} error: {ex.Message}");
                        }
                    });
                    await Task.WhenAll(variantTasks);

                    // Build address from available fields
                    string? brandAddress = link.TraderAddress;
                    if (string.IsNullOrEmpty(brandAddress))
                        brandAddress = link.SupplierLocation;

                    var group = new BrandProductGroup
                    {
                        BrandId = link.BrandId,
                        BrandName = link.BrandName ?? string.Empty,
                        BrandAddress = brandAddress,
                        Products = new ObservableCollection<ProductSummary>(products)
                    };

                    groups.Add(group);
                }

                _allGroups = groups;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBrands] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private void ApplyFilter()
        {
            GroupedBrands.Clear();

            var filter = _searchText.Trim();

            foreach (var group in _allGroups)
            {
                if (string.IsNullOrEmpty(filter))
                {
                    GroupedBrands.Add(group);
                    continue;
                }

                var nameMatch = group.BrandName.Contains(filter, StringComparison.OrdinalIgnoreCase);
                var productMatch = group.Products.Any(p =>
                    p.ProductName.Contains(filter, StringComparison.OrdinalIgnoreCase));

                if (nameMatch || productMatch)
                    GroupedBrands.Add(group);
            }

            OnPropertyChanged(nameof(HasBrands));
            OnPropertyChanged(nameof(TotalCount));
        }

        private async Task OpenProductDrawer(ProductSummary? product)
        {
            if (product == null) return;

            // Reset all detail fields
            DetailProductName = null;
            DetailArticleNumber = null;
            DetailCategory = null;
            DetailDescription = null;
            DetailGtinType = null;
            DetailGtin = null;
            DetailProductGroup = null;
            DetailTypeItem = null;
            DetailAgeGroup = null;
            DetailGender = null;
            DetailMarketSegment = null;
            DetailCareImageUrl = null;
            DetailCareText = null;
            DetailSafetyInfo = null;
            DetailHarmfulSubstances = null;
            DetailHarmfulSubstancesInfo = null;
            DetailCertifications = null;
            DetailCertificationsValidation = null;
            DetailChemicalComplianceStandard = null;
            DetailChemicalComplianceValidation = null;
            DetailChemicalComplianceLink = null;
            DetailMicrofibers = null;
            DetailTraceabilityProvider = null;
            DetailPerformance = null;
            DetailRecyclability = null;
            DetailTakeBackInstructions = null;
            DetailRecyclingInstructions = null;
            DetailDisassemblyInstructionsSorters = null;
            DetailDisassemblyInstructionsUser = null;
            DetailCircularDesignStrategy = null;
            DetailCircularDesignDescription = null;
            DetailRepairInstructions = null;
            DetailBrandStatement = null;
            DetailStatementLink = null;
            DetailEnvironmentalFootprint = null;
            DetailVariants.Clear();
            DetailComponents.Clear();

            try
            {
                // Load product detail
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{product.Id}", App.Session!.SupplierKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        var detail = JsonSerializer.Deserialize<ProductDetail>(data.GetRawText(), JsonOptions);
                        if (detail != null)
                        {
                            DetailProductName = detail.ProductName;
                            DetailArticleNumber = detail.ArticleNumber;
                            DetailCategory = detail.Category;
                            DetailDescription = detail.Description;
                            DetailGtinType = detail.GtinType;
                            DetailGtin = detail.Gtin;
                            DetailProductGroup = detail.ProductGroup;
                            DetailTypeItem = detail.TypeItem;
                            DetailAgeGroup = detail.AgeGroup;
                            DetailGender = detail.Gender;
                            DetailMarketSegment = detail.MarketSegment;

                            // Care
                            DetailCareImageUrl = detail.Care?.CareImageUrl;
                            DetailCareText = detail.Care?.CareText;
                            DetailSafetyInfo = detail.Care?.SafetyInformation;

                            // Compliance
                            DetailHarmfulSubstances = detail.Compliance?.HarmfulSubstances;
                            DetailHarmfulSubstancesInfo = detail.Compliance?.HarmfulSubstancesInfo;
                            DetailCertifications = detail.Compliance?.Certifications;
                            DetailCertificationsValidation = detail.Compliance?.CertificationsValidation;
                            DetailChemicalComplianceStandard = detail.Compliance?.ChemicalComplianceStandard;
                            DetailChemicalComplianceValidation = detail.Compliance?.ChemicalComplianceValidation;
                            DetailChemicalComplianceLink = detail.Compliance?.ChemicalComplianceLink;
                            DetailMicrofibers = detail.Compliance?.Microfibers;
                            DetailTraceabilityProvider = detail.Compliance?.TraceabilityProvider;

                            // Circularity
                            DetailPerformance = detail.Circularity?.Performance;
                            DetailRecyclability = detail.Circularity?.Recyclability;
                            DetailTakeBackInstructions = detail.Circularity?.TakeBackInstructions;
                            DetailRecyclingInstructions = detail.Circularity?.RecyclingInstructions;
                            DetailDisassemblyInstructionsSorters = detail.Circularity?.DisassemblyInstructionsSorters;
                            DetailDisassemblyInstructionsUser = detail.Circularity?.DisassemblyInstructionsUser;
                            DetailCircularDesignStrategy = detail.Circularity?.CircularDesignStrategy;
                            DetailCircularDesignDescription = detail.Circularity?.CircularDesignDescription;
                            DetailRepairInstructions = detail.Circularity?.RepairInstructions;

                            // Sustainability
                            DetailBrandStatement = detail.Sustainability?.BrandStatement;
                            DetailStatementLink = detail.Sustainability?.StatementLink;
                            DetailEnvironmentalFootprint = detail.Sustainability?.EnvironmentalFootprint;

                            // Components from detail
                            if (detail.Components != null)
                            {
                                foreach (var c in detail.Components)
                                    DetailComponents.Add(c);
                            }
                        }
                    }
                }

                // Load variants from dedicated endpoint
                var varJson = await _apiClient.GetWithTenantKeyAsync($"/api/products/{product.Id}/variants", App.Session!.SupplierKey!);
                if (varJson != null)
                {
                    using var varDoc = JsonDocument.Parse(varJson);
                    if (varDoc.RootElement.TryGetProperty("data", out var varArr))
                    {
                        var varItems = JsonSerializer.Deserialize<List<VariantInfo>>(varArr.GetRawText(), JsonOptions);
                        if (varItems != null)
                        {
                            foreach (var v in varItems)
                                DetailVariants.Add(v);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SupplierBrands] OpenProductDrawer error: {ex.Message}");
            }

            DrawerTitle = Application.Current.TryFindResource("Drawer_ProductDetail") as string ?? "Produktdetaljer";
            SetDrawerMode(SupplierBrandDrawerMode.ProductDetail);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(SupplierBrandDrawerMode.None);
        }

        private void SetDrawerMode(SupplierBrandDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(DrawerTitle));
            OnPropertyChanged(nameof(HasVariants));
            OnPropertyChanged(nameof(HasComponents));
            OnPropertyChanged(nameof(HasCare));
            OnPropertyChanged(nameof(HasCompliance));
            OnPropertyChanged(nameof(HasCircularity));
            OnPropertyChanged(nameof(HasSustainability));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Helper class for deserializing brand-supplier link response
        private class BrandSupplierLink
        {
            [JsonPropertyName("brand_id")]
            public int BrandId { get; set; }

            [JsonPropertyName("brand_name")]
            public string? BrandName { get; set; }

            [JsonPropertyName("trader_address")]
            public string? TraderAddress { get; set; }

            [JsonPropertyName("supplier_location")]
            public string? SupplierLocation { get; set; }
        }
    }
}
