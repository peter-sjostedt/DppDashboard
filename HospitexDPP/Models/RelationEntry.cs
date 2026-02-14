using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class RelationEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("brand_id")]
        public int BrandId { get; set; }

        [JsonPropertyName("supplier_id")]
        public int SupplierId { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        // Enriched from lookups
        public string? BrandName { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierLocation { get; set; }
    }
}
