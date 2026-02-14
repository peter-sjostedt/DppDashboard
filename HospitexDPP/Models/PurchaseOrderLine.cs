using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class PurchaseOrderLine
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchase_order_id")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("product_variant_id")]
        public int ProductVariantId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

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
