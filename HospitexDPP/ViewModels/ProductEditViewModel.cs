using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using HospitexDPP.Models;

namespace HospitexDPP.ViewModels
{
    public class ProductEditViewModel : INotifyPropertyChanged
    {
        private readonly int? _productId;
        private readonly int _brandId;
        private readonly string _tenantApiKey;

        private string _productName = string.Empty;
        private string _description = string.Empty;
        private string _photoUrl = string.Empty;
        private string _articleNumber = string.Empty;
        private string _gtinType = string.Empty;
        private string _gtin = string.Empty;
        private string _commodityCodeSystem = string.Empty;
        private string _commodityCodeNumber = string.Empty;
        private string _yearOfSale = string.Empty;
        private string _seasonOfSale = string.Empty;
        private string _priceCurrency = string.Empty;
        private string _msrp = string.Empty;
        private string _resalePrice = string.Empty;
        private string _category = string.Empty;
        private string _productGroup = string.Empty;
        private string _typeLineConcept = string.Empty;
        private string _typeItem = string.Empty;
        private string _ageGroup = string.Empty;
        private string _gender = string.Empty;
        private string _marketSegment = string.Empty;
        private string _waterProperties = string.Empty;
        private string _netWeight = string.Empty;
        private string _weightUnit = string.Empty;
        private string _dataCarrierType = string.Empty;
        private string _dataCarrierMaterial = string.Empty;
        private string _dataCarrierLocation = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isSaving;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<bool>? RequestClose;

        public ProductEditViewModel(ProductDetail? product, int brandId, string tenantApiKey)
        {
            _productId = product?.Id;
            _brandId = brandId;
            _tenantApiKey = tenantApiKey;
            IsNew = product == null;
            DialogTitle = product == null ? "Ny produkt" : $"Redigera: {product.ProductName}";

            if (product != null)
            {
                _productName = product.ProductName ?? string.Empty;
                _description = product.Description ?? string.Empty;
                _photoUrl = product.PhotoUrl ?? string.Empty;
                _articleNumber = product.ArticleNumber ?? string.Empty;
                _gtinType = product.GtinType ?? string.Empty;
                _gtin = product.Gtin ?? string.Empty;
                _commodityCodeSystem = product.CommodityCodeSystem ?? string.Empty;
                _commodityCodeNumber = product.CommodityCodeNumber ?? string.Empty;
                _yearOfSale = JsonElementToString(product.YearOfSale);
                _seasonOfSale = product.SeasonOfSale ?? string.Empty;
                _priceCurrency = product.PriceCurrency ?? string.Empty;
                _msrp = JsonElementToString(product.Msrp);
                _resalePrice = JsonElementToString(product.ResalePrice);
                _category = product.Category ?? string.Empty;
                _productGroup = product.ProductGroup ?? string.Empty;
                _typeLineConcept = product.TypeLineConcept ?? string.Empty;
                _typeItem = product.TypeItem ?? string.Empty;
                _ageGroup = product.AgeGroup ?? string.Empty;
                _gender = product.Gender ?? string.Empty;
                _marketSegment = product.MarketSegment ?? string.Empty;
                _waterProperties = product.WaterProperties ?? string.Empty;
                _netWeight = JsonElementToString(product.NetWeight);
                _weightUnit = product.WeightUnit ?? string.Empty;
                _dataCarrierType = product.DataCarrierType ?? string.Empty;
                _dataCarrierMaterial = product.DataCarrierMaterial ?? string.Empty;
                _dataCarrierLocation = product.DataCarrierLocation ?? string.Empty;
            }

            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !string.IsNullOrWhiteSpace(ProductName) && !IsSaving);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }

        public bool IsNew { get; }
        public string DialogTitle { get; }

        public string ProductName { get => _productName; set { _productName = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string PhotoUrl { get => _photoUrl; set { _photoUrl = value; OnPropertyChanged(); } }
        public string ArticleNumber { get => _articleNumber; set { _articleNumber = value; OnPropertyChanged(); } }
        public string GtinType { get => _gtinType; set { _gtinType = value; OnPropertyChanged(); } }
        public string Gtin { get => _gtin; set { _gtin = value; OnPropertyChanged(); } }
        public string CommodityCodeSystem { get => _commodityCodeSystem; set { _commodityCodeSystem = value; OnPropertyChanged(); } }
        public string CommodityCodeNumber { get => _commodityCodeNumber; set { _commodityCodeNumber = value; OnPropertyChanged(); } }
        public string YearOfSale { get => _yearOfSale; set { _yearOfSale = value; OnPropertyChanged(); } }
        public string SeasonOfSale { get => _seasonOfSale; set { _seasonOfSale = value; OnPropertyChanged(); } }
        public string PriceCurrency { get => _priceCurrency; set { _priceCurrency = value; OnPropertyChanged(); } }
        public string Msrp { get => _msrp; set { _msrp = value; OnPropertyChanged(); } }
        public string ResalePrice { get => _resalePrice; set { _resalePrice = value; OnPropertyChanged(); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(); } }
        public string ProductGroup { get => _productGroup; set { _productGroup = value; OnPropertyChanged(); } }
        public string TypeLineConcept { get => _typeLineConcept; set { _typeLineConcept = value; OnPropertyChanged(); } }
        public string TypeItem { get => _typeItem; set { _typeItem = value; OnPropertyChanged(); } }
        public string AgeGroup { get => _ageGroup; set { _ageGroup = value; OnPropertyChanged(); } }
        public string Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }
        public string MarketSegment { get => _marketSegment; set { _marketSegment = value; OnPropertyChanged(); } }
        public string WaterProperties { get => _waterProperties; set { _waterProperties = value; OnPropertyChanged(); } }
        public string NetWeight { get => _netWeight; set { _netWeight = value; OnPropertyChanged(); } }
        public string WeightUnit { get => _weightUnit; set { _weightUnit = value; OnPropertyChanged(); } }
        public string DataCarrierType { get => _dataCarrierType; set { _dataCarrierType = value; OnPropertyChanged(); } }
        public string DataCarrierMaterial { get => _dataCarrierMaterial; set { _dataCarrierMaterial = value; OnPropertyChanged(); } }
        public string DataCarrierLocation { get => _dataCarrierLocation; set { _dataCarrierLocation = value; OnPropertyChanged(); } }

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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task SaveAsync()
        {
            IsSaving = true;
            StatusMessage = "Sparar...";

            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["product_name"] = ProductName.Trim(),
                    ["description"] = NullIfEmpty(Description),
                    ["photo_url"] = NullIfEmpty(PhotoUrl),
                    ["article_number"] = NullIfEmpty(ArticleNumber),
                    ["gtin_type"] = NullIfEmpty(GtinType),
                    ["gtin"] = NullIfEmpty(Gtin),
                    ["commodity_code_system"] = NullIfEmpty(CommodityCodeSystem),
                    ["commodity_code_number"] = NullIfEmpty(CommodityCodeNumber),
                    ["year_of_sale"] = ParseIntOrNull(YearOfSale),
                    ["season_of_sale"] = NullIfEmpty(SeasonOfSale),
                    ["price_currency"] = NullIfEmpty(PriceCurrency),
                    ["msrp"] = ParseDecimalOrNull(Msrp),
                    ["resale_price"] = ParseDecimalOrNull(ResalePrice),
                    ["category"] = NullIfEmpty(Category),
                    ["product_group"] = NullIfEmpty(ProductGroup),
                    ["type_line_concept"] = NullIfEmpty(TypeLineConcept),
                    ["type_item"] = NullIfEmpty(TypeItem),
                    ["age_group"] = NullIfEmpty(AgeGroup),
                    ["gender"] = NullIfEmpty(Gender),
                    ["market_segment"] = NullIfEmpty(MarketSegment),
                    ["water_properties"] = NullIfEmpty(WaterProperties),
                    ["net_weight"] = ParseDecimalOrNull(NetWeight),
                    ["weight_unit"] = NullIfEmpty(WeightUnit),
                    ["data_carrier_type"] = NullIfEmpty(DataCarrierType),
                    ["data_carrier_material"] = NullIfEmpty(DataCarrierMaterial),
                    ["data_carrier_location"] = NullIfEmpty(DataCarrierLocation),
                };

                string? result;
                if (IsNew)
                {
                    Debug.WriteLine($"[ProductEdit] POST /api/brands/{_brandId}/products");
                    result = await App.ApiClient.PostWithTenantKeyAsync($"/api/brands/{_brandId}/products", payload, _tenantApiKey);
                }
                else
                {
                    Debug.WriteLine($"[ProductEdit] PUT /api/products/{_productId}");
                    result = await App.ApiClient.PutWithTenantKeyAsync($"/api/products/{_productId}", payload, _tenantApiKey);
                }

                Debug.WriteLine($"[ProductEdit] Response: {result}");

                if (result != null)
                {
                    using var doc = JsonDocument.Parse(result);
                    if (doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        RequestClose?.Invoke(true);
                        return;
                    }
                    if (doc.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        StatusMessage = $"Fel: {errorProp.GetString()}";
                        return;
                    }
                }

                StatusMessage = "Fel: Inget svar från servern";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                Debug.WriteLine($"[ProductEdit] ERROR: {ex}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static string? NullIfEmpty(string value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private static object? ParseIntOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value.Trim(), out var n) ? n : null;
        }

        private static object? ParseDecimalOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        private static string JsonElementToString(JsonElement? element)
        {
            if (element == null || element.Value.ValueKind == JsonValueKind.Null || element.Value.ValueKind == JsonValueKind.Undefined)
                return string.Empty;
            return element.Value.ToString();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
