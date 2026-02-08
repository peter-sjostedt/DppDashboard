using System.Text.Json.Serialization;

namespace DppDashboard.Models
{
    public class CircularityInfo
    {
        [JsonPropertyName("performance")]
        public string? Performance { get; set; }

        [JsonPropertyName("recyclability")]
        public string? Recyclability { get; set; }

        [JsonPropertyName("take_back_instructions")]
        public string? TakeBackInstructions { get; set; }

        [JsonPropertyName("recycling_instructions")]
        public string? RecyclingInstructions { get; set; }

        [JsonPropertyName("disassembly_instructions_sorters")]
        public string? DisassemblyInstructionsSorters { get; set; }

        [JsonPropertyName("disassembly_instructions_user")]
        public string? DisassemblyInstructionsUser { get; set; }

        [JsonPropertyName("circular_design_strategy")]
        public string? CircularDesignStrategy { get; set; }

        [JsonPropertyName("circular_design_description")]
        public string? CircularDesignDescription { get; set; }

        [JsonPropertyName("repair_instructions")]
        public string? RepairInstructions { get; set; }
    }
}
