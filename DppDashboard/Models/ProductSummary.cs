using System.Text.Json.Serialization;

namespace DppDashboard.Models
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
    }
}
