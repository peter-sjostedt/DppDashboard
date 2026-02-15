using System.Text.Json;
using System.Text.Json.Serialization;
using HospitexDPP.Resources;

namespace HospitexDPP.Models
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

        public bool IsRecycled
        {
            get
            {
                if (Recycled == null) return false;
                var el = Recycled.Value;
                return el.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.Number => el.GetInt32() != 0,
                    JsonValueKind.String => el.GetString() is "1" or "true",
                    _ => false
                };
            }
        }

        public string RecycledDisplay
        {
            get
            {
                if (!IsRecycled)
                    return Strings.Value_No;

                if (RecycledPercentage != null)
                {
                    var el = RecycledPercentage.Value;
                    var pct = el.ValueKind switch
                    {
                        JsonValueKind.Number => el.ToString()!,
                        JsonValueKind.String => el.GetString() ?? "",
                        _ => ""
                    };
                    if (!string.IsNullOrEmpty(pct))
                        return $"{pct} %";
                }

                return Strings.Value_Yes;
            }
        }
    }
}
