using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class BatchUsage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batch_number")]
        public string? BatchNumber { get; set; }

        [JsonPropertyName("po_number")]
        public string? PoNumber { get; set; }

        [JsonPropertyName("component")]
        public string? Component { get; set; }

        [JsonPropertyName("_status")]
        public string? Status { get; set; }

        [JsonPropertyName("production_date")]
        public string? ProductionDate { get; set; }
    }
}
