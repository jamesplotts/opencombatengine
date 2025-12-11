using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists

namespace OpenCombatEngine.Implementation.Open5e.Models
{
    public class Open5eSpell
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";
        
        [JsonPropertyName("higher_level")]
        public string HigherLevel { get; set; } = "";

        [JsonPropertyName("range")]
        public string Range { get; set; } = "";
        
        [JsonPropertyName("components")]
        public string Components { get; set; } = "";
        
        [JsonPropertyName("material")]
        public string Material { get; set; } = "";
        
        [JsonPropertyName("ritual")]
        public string Ritual { get; set; } = "";
        
        [JsonPropertyName("duration")]
        public string Duration { get; set; } = "";
        
        [JsonPropertyName("concentration")]
        public string Concentration { get; set; } = "";
        
        [JsonPropertyName("casting_time")]
        public string CastingTime { get; set; } = "";
        
        [JsonPropertyName("level")]
        public string Level { get; set; } = "";

        [JsonPropertyName("level_int")]
        public int LevelInt { get; set; }

        [JsonPropertyName("school")]
        public string School { get; set; } = "";
        
        [JsonPropertyName("dnd_class")]
        public string DndClass { get; set; } = "";
    }

    public class Open5eMonster
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("size")]
        public string Size { get; set; } = "";
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
        
        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = "";
        
        [JsonPropertyName("armor_class")]
        public int ArmorClass { get; set; }
        
        [JsonPropertyName("hit_points")]
        public int HitPoints { get; set; }
        
        [JsonPropertyName("hit_dice")]
        public string HitDice { get; set; } = "";
        
        [JsonPropertyName("speed")]
        public Dictionary<string, object> Speed { get; set; } = new();

        [JsonPropertyName("strength")]
        public int Strength { get; set; }
        
        [JsonPropertyName("dexterity")]
        public int Dexterity { get; set; }
        
        [JsonPropertyName("constitution")]
        public int Constitution { get; set; }
        
        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }
        
        [JsonPropertyName("wisdom")]
        public int Wisdom { get; set; }
        
        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }
        
        [JsonPropertyName("actions")]
        public List<Open5eMonsterAction>? Actions { get; set; }
    }

    public class Open5eMonsterAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";
        
        [JsonPropertyName("attack_bonus")]
        public int? AttackBonus { get; set; }
        
        [JsonPropertyName("damage_dice")]
        public string? DamageDice { get; set; }
    }
}
