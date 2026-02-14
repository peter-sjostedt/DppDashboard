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
    public enum ProductDrawerMode { None, New, Edit }

    public class BrandProductsViewModel : INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ApiClient _apiClient;
        private List<ProductSummary> _allProducts = new();
        private string _searchText = string.Empty;
        private List<StatusFilterOption> _statusFilterOptions = StatusFilterOption.All;

        private ProductSummary? _selectedProduct;
        private ProductDrawerMode _drawerMode = ProductDrawerMode.None;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        // Drawer edit fields
        private int? _editProductId;
        private string _editProductName = string.Empty;
        private string _editGtinType = string.Empty;
        private string _editGtin = string.Empty;
        private string _editDescription = string.Empty;
        private string _editPhotoUrl = string.Empty;
        private string _editArticleNumber = string.Empty;
        private string _editCommodityCodeSystem = string.Empty;
        private string _editCommodityCodeNumber = string.Empty;
        private string _editYearOfSale = string.Empty;
        private string _editSeasonOfSale = string.Empty;
        private string _editPriceCurrency = string.Empty;
        private string _editMsrp = string.Empty;
        private string _editResalePrice = string.Empty;
        private string _editCategory = string.Empty;
        private string _editProductGroup = string.Empty;
        private string _editTypeLineConcept = string.Empty;
        private string _editTypeItem = string.Empty;
        private string _editAgeGroup = string.Empty;
        private string _editGender = string.Empty;
        private string _editMarketSegment = string.Empty;
        private string _editWaterProperties = string.Empty;
        private string _editNetWeight = string.Empty;
        private string _editWeightUnit = string.Empty;
        private string _editDataCarrierType = string.Empty;
        private string _editDataCarrierMaterial = string.Empty;
        private string _editDataCarrierLocation = string.Empty;
        private bool _editIsActive = true;

        // Variant/Component collections
        private ObservableCollection<VariantInfo> _variants = new();
        private ObservableCollection<ComponentInfo> _components = new();

        // Care fields
        private string _editCareImageUrl = string.Empty;
        private string _editCareText = string.Empty;
        private string _editSafetyInfo = string.Empty;

        // Compliance fields
        private string _editHarmfulSubstances = string.Empty;
        private string _editHarmfulSubstancesInfo = string.Empty;
        private string _editCertifications = string.Empty;
        private string _editCertificationsValidation = string.Empty;
        private string _editChemicalComplianceStandard = string.Empty;
        private string _editChemicalComplianceValidation = string.Empty;
        private string _editChemicalComplianceLink = string.Empty;
        private string _editMicrofibers = string.Empty;
        private string _editTraceabilityProvider = string.Empty;

        // Circularity fields
        private string _editPerformance = string.Empty;
        private string _editRecyclability = string.Empty;
        private string _editTakeBackInstructions = string.Empty;
        private string _editRecyclingInstructions = string.Empty;
        private string _editDisassemblyInstructionsSorters = string.Empty;
        private string _editDisassemblyInstructionsUser = string.Empty;
        private string _editCircularDesignStrategy = string.Empty;
        private string _editCircularDesignDescription = string.Empty;
        private string _editRepairInstructions = string.Empty;

        // Sustainability fields
        private string _editBrandStatement = string.Empty;
        private string _editStatementLink = string.Empty;
        private string _editEnvironmentalFootprint = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised after products are loaded/reloaded so parent can update counts.</summary>
        public Action? OnDataChanged;

        public BrandProductsViewModel()
        {
            _apiClient = App.ApiClient;

            AddCommand = new RelayCommand(_ => OpenNewDrawer());
            EditCommand = new RelayCommand(p => OpenEditDrawer(p as ProductSummary));
            DeleteCommand = new RelayCommand(async p => await DeleteProductAsync(p as ProductSummary));
            ToggleActiveCommand = new RelayCommand(async p => await ToggleActiveAsync(p as ProductSummary));
            SaveCommand = new RelayCommand(async _ => await SaveProductAsync(), _ => !string.IsNullOrWhiteSpace(EditProductName) && !IsSaving);
            CancelDrawerCommand = new RelayCommand(_ => CloseDrawer());
            AddVariantCommand = new RelayCommand(async _ => await AddVariantAsync());
            DeleteVariantCommand = new RelayCommand(async p => await DeleteVariantAsync(p as VariantInfo));
            AddComponentCommand = new RelayCommand(async _ => await AddComponentAsync());
            DeleteComponentCommand = new RelayCommand(async p => await DeleteComponentAsync(p as ComponentInfo));
            SaveCareCommand = new RelayCommand(async _ => await SaveCareAsync());
            SaveComplianceCommand = new RelayCommand(async _ => await SaveComplianceAsync());
            SaveCircularityCommand = new RelayCommand(async _ => await SaveCircularityAsync());
            SaveSustainabilityCommand = new RelayCommand(async _ => await SaveSustainabilityAsync());

            var saved = SettingsService.LoadFilter("brand_products");
            if (saved != null)
            {
                var selected = saved.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var opt in _statusFilterOptions)
                    opt.IsSelected = selected.Contains(opt.Value);
            }
            foreach (var opt in _statusFilterOptions)
                opt.PropertyChanged += OnFilterChanged;
            LanguageService.LanguageChanged += OnLanguageChanged;
            _ = LoadProductsAsync();
        }

        public ObservableCollection<ProductSummary> Products { get; } = new();

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
                SettingsService.SaveFilter("brand_products", string.Join(",", sel));
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

        public ProductSummary? SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public bool IsDrawerOpen => _drawerMode != ProductDrawerMode.None;
        public bool IsEditMode => _drawerMode == ProductDrawerMode.Edit;
        public bool ShowIsActive => _drawerMode == ProductDrawerMode.Edit;
        public bool HasProducts => Products.Count > 0;
        public int TotalCount => _allProducts.Count;

        public string DrawerTitle
        {
            get => _drawerMode switch
            {
                ProductDrawerMode.New => Application.Current.TryFindResource("Drawer_NewProduct") as string ?? "Ny produkt",
                ProductDrawerMode.Edit => Application.Current.TryFindResource("Drawer_EditProduct") as string ?? "Redigera produkt",
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

        // Edit properties - product fields
        public string EditProductName
        {
            get => _editProductName;
            set { _editProductName = value; OnPropertyChanged(); }
        }

        public string EditGtinType
        {
            get => _editGtinType;
            set { _editGtinType = value; OnPropertyChanged(); }
        }

        public string EditGtin
        {
            get => _editGtin;
            set { _editGtin = value; OnPropertyChanged(); }
        }

        public string EditDescription
        {
            get => _editDescription;
            set { _editDescription = value; OnPropertyChanged(); }
        }

        public string EditPhotoUrl
        {
            get => _editPhotoUrl;
            set { _editPhotoUrl = value; OnPropertyChanged(); }
        }

        public string EditArticleNumber
        {
            get => _editArticleNumber;
            set { _editArticleNumber = value; OnPropertyChanged(); }
        }

        public string EditCommodityCodeSystem
        {
            get => _editCommodityCodeSystem;
            set { _editCommodityCodeSystem = value; OnPropertyChanged(); }
        }

        public string EditCommodityCodeNumber
        {
            get => _editCommodityCodeNumber;
            set { _editCommodityCodeNumber = value; OnPropertyChanged(); }
        }

        public string EditYearOfSale
        {
            get => _editYearOfSale;
            set { _editYearOfSale = value; OnPropertyChanged(); }
        }

        public string EditSeasonOfSale
        {
            get => _editSeasonOfSale;
            set { _editSeasonOfSale = value; OnPropertyChanged(); }
        }

        public string EditPriceCurrency
        {
            get => _editPriceCurrency;
            set { _editPriceCurrency = value; OnPropertyChanged(); }
        }

        public string EditMsrp
        {
            get => _editMsrp;
            set { _editMsrp = value; OnPropertyChanged(); }
        }

        public string EditResalePrice
        {
            get => _editResalePrice;
            set { _editResalePrice = value; OnPropertyChanged(); }
        }

        public string EditCategory
        {
            get => _editCategory;
            set { _editCategory = value; OnPropertyChanged(); }
        }

        public string EditProductGroup
        {
            get => _editProductGroup;
            set { _editProductGroup = value; OnPropertyChanged(); }
        }

        public string EditTypeLineConcept
        {
            get => _editTypeLineConcept;
            set { _editTypeLineConcept = value; OnPropertyChanged(); }
        }

        public string EditTypeItem
        {
            get => _editTypeItem;
            set { _editTypeItem = value; OnPropertyChanged(); }
        }

        public string EditAgeGroup
        {
            get => _editAgeGroup;
            set { _editAgeGroup = value; OnPropertyChanged(); }
        }

        public string EditGender
        {
            get => _editGender;
            set { _editGender = value; OnPropertyChanged(); }
        }

        public string EditMarketSegment
        {
            get => _editMarketSegment;
            set { _editMarketSegment = value; OnPropertyChanged(); }
        }

        public string EditWaterProperties
        {
            get => _editWaterProperties;
            set { _editWaterProperties = value; OnPropertyChanged(); }
        }

        public string EditNetWeight
        {
            get => _editNetWeight;
            set { _editNetWeight = value; OnPropertyChanged(); }
        }

        public string EditWeightUnit
        {
            get => _editWeightUnit;
            set { _editWeightUnit = value; OnPropertyChanged(); }
        }

        public string EditDataCarrierType
        {
            get => _editDataCarrierType;
            set { _editDataCarrierType = value; OnPropertyChanged(); }
        }

        public string EditDataCarrierMaterial
        {
            get => _editDataCarrierMaterial;
            set { _editDataCarrierMaterial = value; OnPropertyChanged(); }
        }

        public string EditDataCarrierLocation
        {
            get => _editDataCarrierLocation;
            set { _editDataCarrierLocation = value; OnPropertyChanged(); }
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set { _editIsActive = value; OnPropertyChanged(); }
        }

        // Variant/Component collections
        public ObservableCollection<VariantInfo> Variants
        {
            get => _variants;
        }

        public ObservableCollection<ComponentInfo> Components
        {
            get => _components;
        }

        // Care edit properties
        public string EditCareImageUrl
        {
            get => _editCareImageUrl;
            set { _editCareImageUrl = value; OnPropertyChanged(); }
        }

        public string EditCareText
        {
            get => _editCareText;
            set { _editCareText = value; OnPropertyChanged(); }
        }

        public string EditSafetyInfo
        {
            get => _editSafetyInfo;
            set { _editSafetyInfo = value; OnPropertyChanged(); }
        }

        // Compliance edit properties
        public string EditHarmfulSubstances
        {
            get => _editHarmfulSubstances;
            set { _editHarmfulSubstances = value; OnPropertyChanged(); }
        }

        public string EditHarmfulSubstancesInfo
        {
            get => _editHarmfulSubstancesInfo;
            set { _editHarmfulSubstancesInfo = value; OnPropertyChanged(); }
        }

        public string EditCertifications
        {
            get => _editCertifications;
            set { _editCertifications = value; OnPropertyChanged(); }
        }

        public string EditCertificationsValidation
        {
            get => _editCertificationsValidation;
            set { _editCertificationsValidation = value; OnPropertyChanged(); }
        }

        public string EditChemicalComplianceStandard
        {
            get => _editChemicalComplianceStandard;
            set { _editChemicalComplianceStandard = value; OnPropertyChanged(); }
        }

        public string EditChemicalComplianceValidation
        {
            get => _editChemicalComplianceValidation;
            set { _editChemicalComplianceValidation = value; OnPropertyChanged(); }
        }

        public string EditChemicalComplianceLink
        {
            get => _editChemicalComplianceLink;
            set { _editChemicalComplianceLink = value; OnPropertyChanged(); }
        }

        public string EditMicrofibers
        {
            get => _editMicrofibers;
            set { _editMicrofibers = value; OnPropertyChanged(); }
        }

        public string EditTraceabilityProvider
        {
            get => _editTraceabilityProvider;
            set { _editTraceabilityProvider = value; OnPropertyChanged(); }
        }

        // Circularity edit properties
        public string EditPerformance
        {
            get => _editPerformance;
            set { _editPerformance = value; OnPropertyChanged(); }
        }

        public string EditRecyclability
        {
            get => _editRecyclability;
            set { _editRecyclability = value; OnPropertyChanged(); }
        }

        public string EditTakeBackInstructions
        {
            get => _editTakeBackInstructions;
            set { _editTakeBackInstructions = value; OnPropertyChanged(); }
        }

        public string EditRecyclingInstructions
        {
            get => _editRecyclingInstructions;
            set { _editRecyclingInstructions = value; OnPropertyChanged(); }
        }

        public string EditDisassemblyInstructionsSorters
        {
            get => _editDisassemblyInstructionsSorters;
            set { _editDisassemblyInstructionsSorters = value; OnPropertyChanged(); }
        }

        public string EditDisassemblyInstructionsUser
        {
            get => _editDisassemblyInstructionsUser;
            set { _editDisassemblyInstructionsUser = value; OnPropertyChanged(); }
        }

        public string EditCircularDesignStrategy
        {
            get => _editCircularDesignStrategy;
            set { _editCircularDesignStrategy = value; OnPropertyChanged(); }
        }

        public string EditCircularDesignDescription
        {
            get => _editCircularDesignDescription;
            set { _editCircularDesignDescription = value; OnPropertyChanged(); }
        }

        public string EditRepairInstructions
        {
            get => _editRepairInstructions;
            set { _editRepairInstructions = value; OnPropertyChanged(); }
        }

        // Sustainability edit properties
        public string EditBrandStatement
        {
            get => _editBrandStatement;
            set { _editBrandStatement = value; OnPropertyChanged(); }
        }

        public string EditStatementLink
        {
            get => _editStatementLink;
            set { _editStatementLink = value; OnPropertyChanged(); }
        }

        public string EditEnvironmentalFootprint
        {
            get => _editEnvironmentalFootprint;
            set { _editEnvironmentalFootprint = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelDrawerCommand { get; }
        public ICommand AddVariantCommand { get; }
        public ICommand DeleteVariantCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand DeleteComponentCommand { get; }
        public ICommand SaveCareCommand { get; }
        public ICommand SaveComplianceCommand { get; }
        public ICommand SaveCircularityCommand { get; }
        public ICommand SaveSustainabilityCommand { get; }

        private void SetDrawerMode(ProductDrawerMode mode)
        {
            _drawerMode = mode;
            OnPropertyChanged(nameof(IsDrawerOpen));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(ShowIsActive));
            OnPropertyChanged(nameof(DrawerTitle));
        }

        private void OpenNewDrawer()
        {
            _editProductId = null;
            EditProductName = string.Empty;
            EditGtinType = string.Empty;
            EditGtin = string.Empty;
            EditDescription = string.Empty;
            EditPhotoUrl = string.Empty;
            EditArticleNumber = string.Empty;
            EditCommodityCodeSystem = string.Empty;
            EditCommodityCodeNumber = string.Empty;
            EditYearOfSale = string.Empty;
            EditSeasonOfSale = string.Empty;
            EditPriceCurrency = string.Empty;
            EditMsrp = string.Empty;
            EditResalePrice = string.Empty;
            EditCategory = string.Empty;
            EditProductGroup = string.Empty;
            EditTypeLineConcept = string.Empty;
            EditTypeItem = string.Empty;
            EditAgeGroup = string.Empty;
            EditGender = string.Empty;
            EditMarketSegment = string.Empty;
            EditWaterProperties = string.Empty;
            EditNetWeight = string.Empty;
            EditWeightUnit = string.Empty;
            EditDataCarrierType = string.Empty;
            EditDataCarrierMaterial = string.Empty;
            EditDataCarrierLocation = string.Empty;
            EditIsActive = true;
            Variants.Clear();
            Components.Clear();
            EditCareImageUrl = string.Empty;
            EditCareText = string.Empty;
            EditSafetyInfo = string.Empty;
            EditHarmfulSubstances = string.Empty;
            EditHarmfulSubstancesInfo = string.Empty;
            EditCertifications = string.Empty;
            EditCertificationsValidation = string.Empty;
            EditChemicalComplianceStandard = string.Empty;
            EditChemicalComplianceValidation = string.Empty;
            EditChemicalComplianceLink = string.Empty;
            EditMicrofibers = string.Empty;
            EditTraceabilityProvider = string.Empty;
            EditPerformance = string.Empty;
            EditRecyclability = string.Empty;
            EditTakeBackInstructions = string.Empty;
            EditRecyclingInstructions = string.Empty;
            EditDisassemblyInstructionsSorters = string.Empty;
            EditDisassemblyInstructionsUser = string.Empty;
            EditCircularDesignStrategy = string.Empty;
            EditCircularDesignDescription = string.Empty;
            EditRepairInstructions = string.Empty;
            EditBrandStatement = string.Empty;
            EditStatementLink = string.Empty;
            EditEnvironmentalFootprint = string.Empty;
            StatusMessage = string.Empty;
            SetDrawerMode(ProductDrawerMode.New);
        }

        private async void OpenEditDrawer(ProductSummary? product)
        {
            if (product == null) return;
            SelectedProduct = product;
            _editProductId = product.Id;
            StatusMessage = string.Empty;

            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{product.Id}", App.Session!.BrandKey!);
                Debug.WriteLine($"[BrandProducts] RAW product detail response: {json}");
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var data))
                    {
                        Debug.WriteLine($"[BrandProducts] data element: {data.GetRawText()}");
                        var detail = JsonSerializer.Deserialize<ProductDetail>(data.GetRawText(), JsonOptions);
                        Debug.WriteLine($"[BrandProducts] Deserialized detail: Name={detail?.ProductName}, Care={detail?.Care != null}, Compliance={detail?.Compliance != null}, Components={detail?.Components?.Count}");
                        if (detail != null)
                        {
                            EditProductName = detail.ProductName ?? string.Empty;
                            EditGtinType = detail.GtinType ?? string.Empty;
                            EditGtin = detail.Gtin ?? string.Empty;
                            EditDescription = detail.Description ?? string.Empty;
                            EditPhotoUrl = detail.PhotoUrl ?? string.Empty;
                            EditArticleNumber = detail.ArticleNumber ?? string.Empty;
                            EditCommodityCodeSystem = detail.CommodityCodeSystem ?? string.Empty;
                            EditCommodityCodeNumber = detail.CommodityCodeNumber ?? string.Empty;
                            EditYearOfSale = detail.YearOfSale?.ToString() ?? string.Empty;
                            EditSeasonOfSale = detail.SeasonOfSale ?? string.Empty;
                            EditPriceCurrency = detail.PriceCurrency ?? string.Empty;
                            EditMsrp = detail.Msrp?.ToString() ?? string.Empty;
                            EditResalePrice = detail.ResalePrice?.ToString() ?? string.Empty;
                            EditCategory = detail.Category ?? string.Empty;
                            EditProductGroup = detail.ProductGroup ?? string.Empty;
                            EditTypeLineConcept = detail.TypeLineConcept ?? string.Empty;
                            EditTypeItem = detail.TypeItem ?? string.Empty;
                            EditAgeGroup = detail.AgeGroup ?? string.Empty;
                            EditGender = detail.Gender ?? string.Empty;
                            EditMarketSegment = detail.MarketSegment ?? string.Empty;
                            EditWaterProperties = detail.WaterProperties ?? string.Empty;
                            EditNetWeight = detail.NetWeight?.ToString() ?? string.Empty;
                            EditWeightUnit = detail.WeightUnit ?? string.Empty;
                            EditDataCarrierType = detail.DataCarrierType ?? string.Empty;
                            EditDataCarrierMaterial = detail.DataCarrierMaterial ?? string.Empty;
                            EditDataCarrierLocation = detail.DataCarrierLocation ?? string.Empty;

                            EditIsActive = detail.IsActive == 1;

                            // Load care (nested object: care_information)
                            EditCareImageUrl = detail.Care?.CareImageUrl ?? string.Empty;
                            EditCareText = detail.Care?.CareText ?? string.Empty;
                            EditSafetyInfo = detail.Care?.SafetyInformation ?? string.Empty;

                            // Load compliance (nested object: compliance_information)
                            EditHarmfulSubstances = detail.Compliance?.HarmfulSubstances ?? string.Empty;
                            EditHarmfulSubstancesInfo = detail.Compliance?.HarmfulSubstancesInfo ?? string.Empty;
                            EditCertifications = detail.Compliance?.Certifications ?? string.Empty;
                            EditCertificationsValidation = detail.Compliance?.CertificationsValidation ?? string.Empty;
                            EditChemicalComplianceStandard = detail.Compliance?.ChemicalComplianceStandard ?? string.Empty;
                            EditChemicalComplianceValidation = detail.Compliance?.ChemicalComplianceValidation ?? string.Empty;
                            EditChemicalComplianceLink = detail.Compliance?.ChemicalComplianceLink ?? string.Empty;
                            EditMicrofibers = detail.Compliance?.Microfibers ?? string.Empty;
                            EditTraceabilityProvider = detail.Compliance?.TraceabilityProvider ?? string.Empty;

                            // Load circularity (nested object: circularity_information)
                            EditPerformance = detail.Circularity?.Performance ?? string.Empty;
                            EditRecyclability = detail.Circularity?.Recyclability ?? string.Empty;
                            EditTakeBackInstructions = detail.Circularity?.TakeBackInstructions ?? string.Empty;
                            EditRecyclingInstructions = detail.Circularity?.RecyclingInstructions ?? string.Empty;
                            EditDisassemblyInstructionsSorters = detail.Circularity?.DisassemblyInstructionsSorters ?? string.Empty;
                            EditDisassemblyInstructionsUser = detail.Circularity?.DisassemblyInstructionsUser ?? string.Empty;
                            EditCircularDesignStrategy = detail.Circularity?.CircularDesignStrategy ?? string.Empty;
                            EditCircularDesignDescription = detail.Circularity?.CircularDesignDescription ?? string.Empty;
                            EditRepairInstructions = detail.Circularity?.RepairInstructions ?? string.Empty;

                            // Load sustainability (nested object: sustainability_information)
                            EditBrandStatement = detail.Sustainability?.BrandStatement ?? string.Empty;
                            EditStatementLink = detail.Sustainability?.StatementLink ?? string.Empty;
                            EditEnvironmentalFootprint = detail.Sustainability?.EnvironmentalFootprint ?? string.Empty;

                            // Load components from detail
                            Components.Clear();
                            if (detail.Components != null)
                            {
                                foreach (var c in detail.Components)
                                    Components.Add(c);
                            }
                        }
                    }
                }

                // Load variants from dedicated endpoint
                Variants.Clear();
                var varJson = await _apiClient.GetWithTenantKeyAsync($"/api/products/{product.Id}/variants", App.Session!.BrandKey!);
                if (varJson != null)
                {
                    using var varDoc = JsonDocument.Parse(varJson);
                    if (varDoc.RootElement.TryGetProperty("data", out var varArr))
                    {
                        var varItems = JsonSerializer.Deserialize<List<VariantInfo>>(varArr.GetRawText(), JsonOptions);
                        if (varItems != null)
                        {
                            foreach (var v in varItems)
                                Variants.Add(v);
                        }
                    }
                }

                // Load components
                Components.Clear();
                var compJson = await _apiClient.GetWithTenantKeyAsync($"/api/products/{product.Id}/components", App.Session!.BrandKey!);
                if (compJson != null)
                {
                    using var compDoc = JsonDocument.Parse(compJson);
                    if (compDoc.RootElement.TryGetProperty("data", out var compArr))
                    {
                        var items = JsonSerializer.Deserialize<List<ComponentInfo>>(compArr.GetRawText(), JsonOptions);
                        if (items != null)
                        {
                            foreach (var c in items)
                                Components.Add(c);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] OpenEditDrawer error: {ex.Message}");
            }

            SetDrawerMode(ProductDrawerMode.Edit);
        }

        private void CloseDrawer()
        {
            SetDrawerMode(ProductDrawerMode.None);
        }

        private async Task SaveProductAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["product_name"] = EditProductName.Trim(),
                    ["gtin_type"] = NullIfEmpty(EditGtinType),
                    ["gtin"] = NullIfEmpty(EditGtin),
                    ["description"] = NullIfEmpty(EditDescription),
                    ["photo_url"] = NullIfEmpty(EditPhotoUrl),
                    ["article_number"] = NullIfEmpty(EditArticleNumber),
                    ["commodity_code_system"] = NullIfEmpty(EditCommodityCodeSystem),
                    ["commodity_code_number"] = NullIfEmpty(EditCommodityCodeNumber),
                    ["year_of_sale"] = NullIfEmpty(EditYearOfSale),
                    ["season_of_sale"] = NullIfEmpty(EditSeasonOfSale),
                    ["price_currency"] = NullIfEmpty(EditPriceCurrency),
                    ["msrp"] = NullIfEmpty(EditMsrp),
                    ["resale_price"] = NullIfEmpty(EditResalePrice),
                    ["category"] = NullIfEmpty(EditCategory),
                    ["product_group"] = NullIfEmpty(EditProductGroup),
                    ["type_line_concept"] = NullIfEmpty(EditTypeLineConcept),
                    ["type_item"] = NullIfEmpty(EditTypeItem),
                    ["age_group"] = NullIfEmpty(EditAgeGroup),
                    ["gender"] = NullIfEmpty(EditGender),
                    ["market_segment"] = NullIfEmpty(EditMarketSegment),
                    ["water_properties"] = NullIfEmpty(EditWaterProperties),
                    ["net_weight"] = NullIfEmpty(EditNetWeight),
                    ["weight_unit"] = NullIfEmpty(EditWeightUnit),
                    ["data_carrier_type"] = NullIfEmpty(EditDataCarrierType),
                    ["data_carrier_material"] = NullIfEmpty(EditDataCarrierMaterial),
                    ["data_carrier_location"] = NullIfEmpty(EditDataCarrierLocation),
                    ["_is_active"] = EditIsActive ? 1 : 0
                };

                string? result;
                if (_drawerMode == ProductDrawerMode.New)
                {
                    result = await _apiClient.PostWithTenantKeyAsync($"/api/brands/{App.Session!.BrandId}/products", payload, App.Session!.BrandKey!);
                }
                else
                {
                    result = await _apiClient.PutWithTenantKeyAsync($"/api/products/{_editProductId}", payload, App.Session!.BrandKey!);
                }

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        CloseDrawer();
                        await ReloadProductsAsync();
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

        private async Task SaveCareAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["care_image_url"] = NullIfEmpty(EditCareImageUrl),
                    ["care_text"] = NullIfEmpty(EditCareText),
                    ["safety_information"] = NullIfEmpty(EditSafetyInfo)
                };

                var result = await _apiClient.PutWithTenantKeyAsync($"/api/products/{_editProductId}/care", payload, App.Session!.BrandKey!);
                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        StatusMessage = string.Empty;
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

        private async Task SaveComplianceAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["harmful_substances"] = NullIfEmpty(EditHarmfulSubstances),
                    ["harmful_substances_info"] = NullIfEmpty(EditHarmfulSubstancesInfo),
                    ["certifications"] = NullIfEmpty(EditCertifications),
                    ["certifications_validation"] = NullIfEmpty(EditCertificationsValidation),
                    ["chemical_compliance_standard"] = NullIfEmpty(EditChemicalComplianceStandard),
                    ["chemical_compliance_validation"] = NullIfEmpty(EditChemicalComplianceValidation),
                    ["chemical_compliance_link"] = NullIfEmpty(EditChemicalComplianceLink),
                    ["microfibers"] = NullIfEmpty(EditMicrofibers),
                    ["traceability_provider"] = NullIfEmpty(EditTraceabilityProvider)
                };

                var result = await _apiClient.PutWithTenantKeyAsync($"/api/products/{_editProductId}/compliance", payload, App.Session!.BrandKey!);
                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        StatusMessage = string.Empty;
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

        private async Task SaveCircularityAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["performance"] = NullIfEmpty(EditPerformance),
                    ["recyclability"] = NullIfEmpty(EditRecyclability),
                    ["take_back_instructions"] = NullIfEmpty(EditTakeBackInstructions),
                    ["recycling_instructions"] = NullIfEmpty(EditRecyclingInstructions),
                    ["disassembly_instructions_sorters"] = NullIfEmpty(EditDisassemblyInstructionsSorters),
                    ["disassembly_instructions_user"] = NullIfEmpty(EditDisassemblyInstructionsUser),
                    ["circular_design_strategy"] = NullIfEmpty(EditCircularDesignStrategy),
                    ["circular_design_description"] = NullIfEmpty(EditCircularDesignDescription),
                    ["repair_instructions"] = NullIfEmpty(EditRepairInstructions)
                };

                var result = await _apiClient.PutWithTenantKeyAsync($"/api/products/{_editProductId}/circularity", payload, App.Session!.BrandKey!);
                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        StatusMessage = string.Empty;
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

        private async Task SaveSustainabilityAsync()
        {
            IsSaving = true;
            StatusMessage = Application.Current.TryFindResource("Msg_Saving") as string ?? "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["brand_statement"] = NullIfEmpty(EditBrandStatement),
                    ["statement_link"] = NullIfEmpty(EditStatementLink),
                    ["environmental_footprint"] = NullIfEmpty(EditEnvironmentalFootprint)
                };

                var result = await _apiClient.PutWithTenantKeyAsync($"/api/products/{_editProductId}/sustainability", payload, App.Session!.BrandKey!);
                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        StatusMessage = string.Empty;
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

        private async Task DeleteProductAsync(ProductSummary? product)
        {
            if (product == null) return;

            var confirmText = Application.Current.TryFindResource("Confirm_Delete") as string ?? "Är du säker?";
            var result = MessageBox.Show(
                $"{confirmText}\n\n{product.ProductName}",
                Application.Current.TryFindResource("Action_Delete") as string ?? "Ta bort",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/products/{product.Id}", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadProductsAsync();
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

        private async Task ToggleActiveAsync(ProductSummary? product)
        {
            if (product == null) return;

            var newActive = product.IsActive == 1 ? 0 : 1;
            var payload = new Dictionary<string, object?>
            {
                ["product_name"] = product.ProductName,
                ["_is_active"] = newActive
            };

            try
            {
                var json = await _apiClient.PutWithTenantKeyAsync($"/api/products/{product.Id}", payload, App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadProductsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] ToggleActive error: {ex.Message}");
            }
        }

        private async Task AddVariantAsync()
        {
            try
            {
                var payload = new Dictionary<string, object?>();
                var json = await _apiClient.PostWithTenantKeyAsync($"/api/products/{_editProductId}/variants", payload, App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadVariantsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] AddVariant error: {ex.Message}");
            }
        }

        private async Task DeleteVariantAsync(VariantInfo? variant)
        {
            if (variant == null) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/variants/{variant.Id}", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadVariantsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] DeleteVariant error: {ex.Message}");
            }
        }

        private async Task AddComponentAsync()
        {
            try
            {
                var payload = new Dictionary<string, object?>();
                var json = await _apiClient.PostWithTenantKeyAsync($"/api/products/{_editProductId}/components", payload, App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadComponentsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] AddComponent error: {ex.Message}");
            }
        }

        private async Task DeleteComponentAsync(ComponentInfo? component)
        {
            if (component == null) return;

            try
            {
                var json = await _apiClient.DeleteWithTenantKeyAsync($"/api/components/{component.Id}", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
                    {
                        await ReloadComponentsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] DeleteComponent error: {ex.Message}");
            }
        }

        private async Task ReloadVariantsAsync()
        {
            Variants.Clear();
            var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{_editProductId}/variants", App.Session!.BrandKey!);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var arr))
                {
                    var items = JsonSerializer.Deserialize<List<VariantInfo>>(arr.GetRawText(), JsonOptions);
                    if (items != null) foreach (var v in items) Variants.Add(v);
                }
            }
        }

        private async Task ReloadComponentsAsync()
        {
            Components.Clear();
            var json = await _apiClient.GetWithTenantKeyAsync($"/api/products/{_editProductId}/components", App.Session!.BrandKey!);
            if (json != null)
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var arr))
                {
                    var items = JsonSerializer.Deserialize<List<ComponentInfo>>(arr.GetRawText(), JsonOptions);
                    if (items != null) foreach (var c in items) Components.Add(c);
                }
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var json = await _apiClient.GetWithTenantKeyAsync("/api/products", App.Session!.BrandKey!);
                if (json != null)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataArray))
                    {
                        var items = JsonSerializer.Deserialize<List<ProductSummary>>(dataArray.GetRawText(), JsonOptions);
                        if (items != null)
                            _allProducts = items;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BrandProducts] Load error: {ex.Message}");
            }

            ApplyFilter();
            OnDataChanged?.Invoke();
        }

        private async Task ReloadProductsAsync()
        {
            _allProducts.Clear();
            Products.Clear();
            await LoadProductsAsync();
        }

        private void ApplyFilter()
        {
            Products.Clear();
            var filter = _searchText.Trim();
            IEnumerable<ProductSummary> filtered = _allProducts;

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(p => p.ProductName != null &&
                    p.ProductName.Contains(filter, StringComparison.OrdinalIgnoreCase));

            var activeFilters = _statusFilterOptions.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
            if (activeFilters.Count > 0)
                filtered = filtered.Where(p =>
                    (activeFilters.Contains("active") && p.IsActive == 1) ||
                    (activeFilters.Contains("inactive") && p.IsActive != 1));

            foreach (var p in filtered)
                Products.Add(p);

            OnPropertyChanged(nameof(HasProducts));
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
