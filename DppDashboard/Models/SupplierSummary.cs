using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class SupplierSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;
    }
}
