using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class BrandSummary
    {
        [JsonIgnore]
        public int SupplierCount { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("brand_name")]
        public string BrandName { get; set; } = string.Empty;

        [JsonPropertyName("logo_url")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("api_key")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("sub_brand")]
        public string? SubBrand { get; set; }

        [JsonPropertyName("parent_company")]
        public string? ParentCompany { get; set; }

        [JsonPropertyName("trader")]
        public string? Trader { get; set; }

        [JsonPropertyName("trader_location")]
        public string? TraderLocation { get; set; }

        [JsonPropertyName("lei")]
        public string? Lei { get; set; }

        [JsonPropertyName("gs1_company_prefix")]
        public string? Gs1CompanyPrefix { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; }

        [JsonPropertyName("product_count")]
        public int ProductCount { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }
}
