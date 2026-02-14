using System.Text.Json;
using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class ProductDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("brand_id")]
        public int BrandId { get; set; }

        [JsonPropertyName("brand_name")]
        public string? BrandName { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("gtin_type")]
        public string? GtinType { get; set; }

        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("photo_url")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("article_number")]
        public string? ArticleNumber { get; set; }

        [JsonPropertyName("commodity_code_system")]
        public string? CommodityCodeSystem { get; set; }

        [JsonPropertyName("commodity_code_number")]
        public string? CommodityCodeNumber { get; set; }

        [JsonPropertyName("year_of_sale")]
        public JsonElement? YearOfSale { get; set; }

        [JsonPropertyName("season_of_sale")]
        public string? SeasonOfSale { get; set; }

        [JsonPropertyName("price_currency")]
        public string? PriceCurrency { get; set; }

        [JsonPropertyName("msrp")]
        public JsonElement? Msrp { get; set; }

        [JsonPropertyName("resale_price")]
        public JsonElement? ResalePrice { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("product_group")]
        public string? ProductGroup { get; set; }

        [JsonPropertyName("type_line_concept")]
        public string? TypeLineConcept { get; set; }

        [JsonPropertyName("type_item")]
        public string? TypeItem { get; set; }

        [JsonPropertyName("age_group")]
        public string? AgeGroup { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("market_segment")]
        public string? MarketSegment { get; set; }

        [JsonPropertyName("water_properties")]
        public string? WaterProperties { get; set; }

        [JsonPropertyName("net_weight")]
        public JsonElement? NetWeight { get; set; }

        [JsonPropertyName("weight_unit")]
        public string? WeightUnit { get; set; }

        [JsonPropertyName("data_carrier_type")]
        public string? DataCarrierType { get; set; }

        [JsonPropertyName("data_carrier_material")]
        public string? DataCarrierMaterial { get; set; }

        [JsonPropertyName("data_carrier_location")]
        public string? DataCarrierLocation { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; } = 1;

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        // Nested objects from API
        [JsonPropertyName("care_information")]
        public CareInfo? Care { get; set; }

        [JsonPropertyName("compliance_information")]
        public ComplianceInfo? Compliance { get; set; }

        [JsonPropertyName("circularity_information")]
        public CircularityInfo? Circularity { get; set; }

        [JsonPropertyName("sustainability_information")]
        public SustainabilityInfo? Sustainability { get; set; }

        [JsonPropertyName("components")]
        public List<ComponentInfo>? Components { get; set; }
    }
}
