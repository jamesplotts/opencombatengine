using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Open5e.Models
{
    public class Open5eWeapon
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
        
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("cost")]
        public string Cost { get; set; } = string.Empty;

        [JsonPropertyName("damage_dice")]
        public string DamageDice { get; set; } = string.Empty;

        [JsonPropertyName("damage_type")]
        public string DamageType { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public string Weight { get; set; } = string.Empty;
        
        [JsonPropertyName("properties")]
        public System.Collections.Generic.IList<string>? Properties { get; } = new System.Collections.Generic.List<string>();
    }
}
