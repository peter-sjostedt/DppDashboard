using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class MaterialCertification
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("certification")]
        public string? Certification { get; set; }

        [JsonPropertyName("certification_id")]
        public string? CertificationId { get; set; }

        [JsonPropertyName("valid_until")]
        public string? ValidUntil { get; set; }
    }
}
