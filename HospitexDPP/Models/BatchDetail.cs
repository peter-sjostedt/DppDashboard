using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class BatchDetail : BatchSummary
    {
        [JsonPropertyName("facility_name")]
        public string? FacilityName { get; set; }

        [JsonPropertyName("facility_location")]
        public string? FacilityLocation { get; set; }

        [JsonPropertyName("facility_registry")]
        public string? FacilityRegistry { get; set; }

        [JsonPropertyName("facility_identifier")]
        public string? FacilityIdentifier { get; set; }

        [JsonPropertyName("country_of_origin_confection")]
        public string? CountryConfection { get; set; }

        [JsonPropertyName("country_of_origin_dyeing")]
        public string? CountryDyeing { get; set; }

        [JsonPropertyName("country_of_origin_weaving")]
        public string? CountryWeaving { get; set; }

        [JsonPropertyName("materials")]
        public List<BatchMaterialInfo>? Materials { get; set; }
    }
}
