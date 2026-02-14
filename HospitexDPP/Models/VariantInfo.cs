using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class VariantInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("item_number")]
        public string? ItemNumber { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("size_country_code")]
        public string? SizeCountryCode { get; set; }

        [JsonPropertyName("color_brand")]
        public string? ColorBrand { get; set; }

        [JsonPropertyName("color_general")]
        public string? ColorGeneral { get; set; }

        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; } = 1;
    }
}
