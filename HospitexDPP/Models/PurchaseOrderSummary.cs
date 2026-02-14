using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class PurchaseOrderSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("brand_id")]
        public int BrandId { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("po_number")]
        public string PoNumber { get; set; } = "";

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("requested_delivery_date")]
        public string? RequestedDeliveryDate { get; set; }

        [JsonPropertyName("_status")]
        public string Status { get; set; } = "draft";

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        // Joined fields from API
        [JsonPropertyName("brand_name")]
        public string? BrandName { get; set; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("batch_count")]
        public int? BatchCount { get; set; }

        [JsonPropertyName("produced_quantity")]
        public int? ProducedQuantity { get; set; }

        // UI helpers
        public string StatusDisplay => Status switch
        {
            "draft" => "Utkast",
            "sent" => "Skickad",
            "accepted" => "Accepterad",
            "fulfilled" => "Levererad",
            "cancelled" => "Avbruten",
            _ => Status
        };

        public string StatusColor => Status switch
        {
            "draft" => "#9E9E9E",
            "sent" => "#FF9800",
            "accepted" => "#4CAF50",
            "fulfilled" => "#2196F3",
            "cancelled" => "#F44336",
            _ => "#9E9E9E"
        };

        public bool CanAccept => Status == "sent";
        public string ProgressText => $"{ProducedQuantity ?? 0} / {Quantity ?? 0}";
    }
}
