using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class SustainabilityInfo
    {
        [JsonPropertyName("brand_statement")]
        public string? BrandStatement { get; set; }

        [JsonPropertyName("statement_link")]
        public string? StatementLink { get; set; }

        [JsonPropertyName("environmental_footprint")]
        public string? EnvironmentalFootprint { get; set; }
    }
}
