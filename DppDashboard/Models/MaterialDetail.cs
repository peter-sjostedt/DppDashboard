using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class MaterialDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("material_name")]
        public string MaterialName { get; set; } = string.Empty;

        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("compositions")]
        public List<MaterialComposition>? Compositions { get; set; }

        [JsonPropertyName("certifications")]
        public List<MaterialCertification>? Certifications { get; set; }

        [JsonPropertyName("supply_chain")]
        public List<MaterialSupplyChainStep>? SupplyChain { get; set; }
    }
}
