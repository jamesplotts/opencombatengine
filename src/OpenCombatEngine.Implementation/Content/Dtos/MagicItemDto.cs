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

        [JsonPropertyName("bonusSavingThrow")]
        public string? BonusSavingThrow { get; set; }
        
        [JsonPropertyName("weight")]
        public double? Weight { get; set; }
        
        [JsonPropertyName("value")]
        public long? Value { get; set; } // Often in cp or gp, need to handle parsing

        [JsonPropertyName("charges")]
        public int? Charges { get; set; }

        [JsonPropertyName("recharge")]
        public string? Recharge { get; set; }

        // Weapon properties
        [JsonPropertyName("dmg1")]
        public string? Dmg1 { get; set; } // Damage dice (e.g. "1d8")
        
        [JsonPropertyName("dmgType")]
        public string? DmgType { get; set; } // Damage type (e.g. "S")
        
        [JsonPropertyName("range")]
        public string? Range { get; set; } // Range (e.g. "20/60")

        // Armor properties
        [JsonPropertyName("ac")]
        public int? Ac { get; set; } // Base AC
        
        [JsonPropertyName("strength")]
        public string? Strength { get; set; } // Strength requirement
        
        [JsonPropertyName("stealth")]
        public bool? Stealth { get; set; } // Disadvantage on stealth?
    }
    
    public class MagicItemCompendiumDto
    {
        [JsonPropertyName("item")]
        public List<MagicItemDto>? Item { get; set; }
    }
#pragma warning restore CA1002 // Do not expose generic lists
#pragma warning restore CA2227 // Collection properties should be read only
}
