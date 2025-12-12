using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Open5e.Models
{
    public class Open5eArmor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
        
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty; // Light, Medium, Heavy, Shield

        [JsonPropertyName("base_ac")]
        public int BaseAc { get; set; }

        [JsonPropertyName("plus_dex_mod")]
        public bool PlusDexMod { get; set; }

        [JsonPropertyName("plus_max")]
        public int? PlusMax { get; set; } // Max Dex bonus (e.g. 2 for Medium)

        [JsonPropertyName("strength_requirement")]
        public int? StrengthRequirement { get; set; }

        [JsonPropertyName("stealth_disadvantage")]
        public bool StealthDisadvantage { get; set; }

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public string Weight { get; set; } = string.Empty;
    }
}
