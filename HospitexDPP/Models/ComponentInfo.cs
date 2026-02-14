using System.Text.Json;
using System.Text.Json.Serialization;

namespace HospitexDPP.Models
{
    public class ComponentInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("component")]
        public string? Component { get; set; }

        [JsonPropertyName("material")]
        public string? Material { get; set; }

        [JsonPropertyName("content_name")]
        public string? ContentName { get; set; }

        [JsonPropertyName("content_value")]
        public JsonElement? ContentValue { get; set; }

        [JsonPropertyName("content_source")]
        public string? ContentSource { get; set; }

        [JsonPropertyName("material_trademarks")]
        public string? MaterialTrademarks { get; set; }

        [JsonPropertyName("content_name_other")]
        public string? ContentNameOther { get; set; }

        [JsonPropertyName("trim_type")]
        public string? TrimType { get; set; }

        [JsonPropertyName("component_weight")]
        public JsonElement? ComponentWeight { get; set; }

        [JsonPropertyName("recycled")]
        public JsonElement? Recycled { get; set; }

        [JsonPropertyName("recycled_percentage")]
        public JsonElement? RecycledPercentage { get; set; }

        [JsonPropertyName("recycled_input_source")]
        public string? RecycledInputSource { get; set; }

        [JsonPropertyName("leather_species")]
        public string? LeatherSpecies { get; set; }

        [JsonPropertyName("leather_grade")]
        public string? LeatherGrade { get; set; }

        [JsonPropertyName("leather_species_other")]
        public string? LeatherSpeciesOther { get; set; }

        [JsonPropertyName("leather_pattern")]
        public string? LeatherPattern { get; set; }

        [JsonPropertyName("leather_thickness")]
        public JsonElement? LeatherThickness { get; set; }

        [JsonPropertyName("leather_max")]
        public JsonElement? LeatherMax { get; set; }

        [JsonPropertyName("leather_min")]
        public JsonElement? LeatherMin { get; set; }

        [JsonPropertyName("sewing_thread_content")]
        public string? SewingThreadContent { get; set; }

        [JsonPropertyName("print_ink_type")]
        public string? PrintInkType { get; set; }

        [JsonPropertyName("dye_class")]
        public string? DyeClass { get; set; }

        [JsonPropertyName("dye_class_standard")]
        public string? DyeClassStandard { get; set; }

        [JsonPropertyName("finishes")]
        public string? Finishes { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }
    }
}
