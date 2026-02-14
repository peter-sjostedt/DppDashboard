using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class BatchMaterialInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("batch_id")]
        public int BatchId { get; set; }

        [JsonPropertyName("factory_material_id")]
        public int FactoryMaterialId { get; set; }

        [JsonPropertyName("component")]
        public string? Component { get; set; }

        // Join fields from factory_materials
        [JsonPropertyName("material_name")]
        public string? MaterialName { get; set; }

        [JsonPropertyName("material_type")]
        public string? MaterialType { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
