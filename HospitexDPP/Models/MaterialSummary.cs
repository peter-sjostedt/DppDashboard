using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class MaterialSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("material_name")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
