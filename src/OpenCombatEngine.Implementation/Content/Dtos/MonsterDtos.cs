using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Content.Dtos
{
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    public class MonsterCompendiumDto
    {
        [JsonPropertyName("monster")]
        public List<MonsterDto> Monster { get; set; } = new();
    }

    public class MonsterDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("size")]
        public object? Size { get; set; } // Can be string or array

        [JsonPropertyName("type")]
        public object? Type { get; set; } // Can be string or object

        [JsonPropertyName("alignment")]
        public object? Alignment { get; set; } // Can be string or array

        [JsonPropertyName("ac")]
        public List<object>? Ac { get; set; } // Array of numbers or objects

        [JsonPropertyName("hp")]
        public HpDto? Hp { get; set; }

        [JsonPropertyName("speed")]
        public object? Speed { get; set; } // Can be object or string

        [JsonPropertyName("str")]
        public int Str { get; set; }

        [JsonPropertyName("dex")]
        public int Dex { get; set; }

        [JsonPropertyName("con")]
        public int Con { get; set; }

        [JsonPropertyName("int")]
        public int Intelligence { get; set; }

        [JsonPropertyName("wis")]
        public int Wis { get; set; }

        [JsonPropertyName("cha")]
        public int Cha { get; set; }

        [JsonPropertyName("action")]
        public List<MonsterActionDto>? Action { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    public class HpDto
    {
        [JsonPropertyName("average")]
        public int Average { get; set; }

        [JsonPropertyName("formula")]
        public string? Formula { get; set; }
    }

    public class MonsterActionDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("entries")]
        public List<object>? Entries { get; set; }
    }
#pragma warning restore CA2227
#pragma warning restore CA1002
}
