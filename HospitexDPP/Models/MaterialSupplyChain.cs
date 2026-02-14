using System.Text.Json;
using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class MaterialSupplyChain
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("sequence")]
        public JsonElement? Sequence { get; set; }

        [JsonPropertyName("process_step")]
        public string? ProcessStep { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("facility_name")]
        public string? FacilityName { get; set; }

        [JsonPropertyName("facility_identifier")]
        public string? FacilityIdentifier { get; set; }
    }
}
