using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class ProductSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("article_number")]
        public string? ArticleNumber { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        [JsonPropertyName("_is_active")]
        public int IsActive { get; set; } = 1;

        [JsonPropertyName("variant_count")]
        public int VariantCount { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }
}
