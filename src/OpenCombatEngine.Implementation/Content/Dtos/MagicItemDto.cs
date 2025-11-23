using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Content.Dtos
{
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    public class MagicItemDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; set; }

        [JsonPropertyName("reqAttune")]
        public object? ReqAttune { get; set; } // Can be bool or string

        [JsonPropertyName("entries")]
        public List<object>? Entries { get; set; }

        [JsonPropertyName("bonusAc")]
        public string? BonusAc { get; set; }

        [JsonPropertyName("bonusWeapon")]
        public string? BonusWeapon { get; set; }
        
        [JsonPropertyName("weight")]
        public double? Weight { get; set; }
        
        [JsonPropertyName("value")]
        public long? Value { get; set; } // Often in cp or gp, need to handle parsing
    }
    
    public class MagicItemCompendiumDto
    {
        [JsonPropertyName("item")]
        public List<MagicItemDto>? Item { get; set; }
    }
#pragma warning restore CA1002 // Do not expose generic lists
#pragma warning restore CA2227 // Collection properties should be read only
}
