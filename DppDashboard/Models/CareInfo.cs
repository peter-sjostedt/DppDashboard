using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class CareInfo
    {
        [JsonPropertyName("care_image_url")]
        public string? CareImageUrl { get; set; }

        [JsonPropertyName("care_text")]
        public string? CareText { get; set; }

        [JsonPropertyName("safety_information")]
        public string? SafetyInformation { get; set; }
    }
}
