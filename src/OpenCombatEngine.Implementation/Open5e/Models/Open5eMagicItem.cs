using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Open5e.Models
{
    public class Open5eMagicItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
        
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // e.g. "Wondrous Item", "Weapon (longsword)"

        [JsonPropertyName("rarity")]
        public string Rarity { get; set; } = string.Empty;

        [JsonPropertyName("requires_attunement")]
        public string RequiresAttunement { get; set; } = string.Empty; // e.g. "requires attunement" or ""
    }
}
