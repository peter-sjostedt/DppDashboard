using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class ComplianceInfo
    {
        [JsonPropertyName("harmful_substances")]
        public string? HarmfulSubstances { get; set; }

        [JsonPropertyName("harmful_substances_info")]
        public string? HarmfulSubstancesInfo { get; set; }

        [JsonPropertyName("certifications")]
        public string? Certifications { get; set; }

        [JsonPropertyName("certifications_validation")]
        public string? CertificationsValidation { get; set; }

        [JsonPropertyName("chemical_compliance_standard")]
        public string? ChemicalComplianceStandard { get; set; }

        [JsonPropertyName("chemical_compliance_validation")]
        public string? ChemicalComplianceValidation { get; set; }

        [JsonPropertyName("chemical_compliance_link")]
        public string? ChemicalComplianceLink { get; set; }

        [JsonPropertyName("microfibers")]
        public string? Microfibers { get; set; }

        [JsonPropertyName("traceability_provider")]
        public string? TraceabilityProvider { get; set; }
    }
}
