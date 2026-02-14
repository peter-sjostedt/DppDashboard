using System.Text.Json.Serialization;
using System.Windows;

namespace HospitexDPP.Models
{
    public class BatchSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batch_number")]
        public string BatchNumber { get; set; } = string.Empty;

        [JsonPropertyName("purchase_order_id")]
        public int PurchaseOrderId { get; set; }

        [JsonPropertyName("po_number")]
        public string? PoNumber { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("brand_name")]
        public string? BrandName { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("supplier_name")]
        public string? SupplierName { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("item_count")]
        public int? ItemCount { get; set; }

        [JsonPropertyName("_status")]
        public string? Status { get; set; }

        [JsonPropertyName("production_date")]
        public string? ProductionDate { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        public string StatusDisplay => Status switch
        {
            "planned" => Application.Current.TryFindResource("BatchFilter_Planned") as string ?? "Planerad",
            "in_production" => Application.Current.TryFindResource("BatchFilter_InProduction") as string ?? "I produktion",
            "completed" => Application.Current.TryFindResource("BatchFilter_Completed") as string ?? "Klar",
            _ => Status ?? ""
        };

        public string StatusColor => Status switch
        {
            "planned" => "#FF9800",
            "in_production" => "#2196F3",
            "completed" => "#4CAF50",
            _ => "#9E9E9E"
        };

        public string QuantityDisplay => Quantity?.ToString() ?? "–";
    }
}
