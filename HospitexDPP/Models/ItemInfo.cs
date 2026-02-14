using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class ItemInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("unique_product_id")]
        public string? UniqueProductId { get; set; }

        [JsonPropertyName("product_variant_id")]
        public int? ProductVariantId { get; set; }

        [JsonPropertyName("serial_number")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("sgtin")]
        public string? Sgtin { get; set; }

        [JsonPropertyName("tid")]
        public string? Tid { get; set; }

        [JsonPropertyName("_status")]
        public string? Status { get; set; }

        [JsonPropertyName("item_number")]
        public string? ItemNumber { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("color_brand")]
        public string? ColorBrand { get; set; }

        [JsonPropertyName("variant_gtin")]
        public string? VariantGtin { get; set; }
    }
}
