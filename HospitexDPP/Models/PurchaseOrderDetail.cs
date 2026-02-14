using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class PurchaseOrderDetail : PurchaseOrderSummary
    {
        [JsonPropertyName("supplier_name")]
        public string? SupplierName { get; set; }

        [JsonPropertyName("lines")]
        public List<PurchaseOrderLine>? Lines { get; set; }

        [JsonPropertyName("batches")]
        public List<PoBatchInfo>? Batches { get; set; }
    }
}
