using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class SupplierDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [JsonPropertyName("supplier_location")]
        public string? SupplierLocation { get; set; }

        [JsonPropertyName("facility_registry")]
        public string? FacilityRegistry { get; set; }

        [JsonPropertyName("facility_identifier")]
        public string? FacilityIdentifier { get; set; }

        [JsonPropertyName("operator_registry")]
        public string? OperatorRegistry { get; set; }

        [JsonPropertyName("operator_identifier")]
        public string? OperatorIdentifier { get; set; }

        [JsonPropertyName("country_of_origin_confection")]
        public string? CountryOfOriginConfection { get; set; }

        [JsonPropertyName("country_of_origin_dyeing")]
        public string? CountryOfOriginDyeing { get; set; }

        [JsonPropertyName("country_of_origin_weaving")]
        public string? CountryOfOriginWeaving { get; set; }

        [JsonPropertyName("lei")]
        public string? Lei { get; set; }

        [JsonPropertyName("gs1_company_prefix")]
        public string? Gs1CompanyPrefix { get; set; }

        [JsonPropertyName("api_key")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; }
    }
}
