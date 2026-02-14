using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class PoBatchInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batch_number")]
        public string BatchNumber { get; set; } = "";

        [JsonPropertyName("production_date")]
        public string? ProductionDate { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("_status")]
        public string Status { get; set; } = "in_production";

        [JsonPropertyName("item_count")]
        public int ItemCount { get; set; }

        [JsonPropertyName("facility_name")]
        public string? FacilityName { get; set; }

        public string StatusColor => Status switch
        {
            "in_production" => "#FF9800",
            "completed" => "#4CAF50",
            _ => "#9E9E9E"
        };

        public string StatusDisplay => Status switch
        {
            "in_production" => "Produktion",
            "completed" => "Klar",
            _ => Status
        };
    }
}
