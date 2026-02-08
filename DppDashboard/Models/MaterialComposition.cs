using System.Text.Json;
using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class MaterialComposition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("content_name")]
        public string? ContentName { get; set; }

        [JsonPropertyName("content_value")]
        public JsonElement? ContentValue { get; set; }

        [JsonPropertyName("content_source")]
        public string? ContentSource { get; set; }

        [JsonPropertyName("recycled")]
        public JsonElement? Recycled { get; set; }

        [JsonPropertyName("recycled_percentage")]
        public JsonElement? RecycledPercentage { get; set; }

        [JsonPropertyName("recycled_input_source")]
        public string? RecycledInputSource { get; set; }
    }
}
