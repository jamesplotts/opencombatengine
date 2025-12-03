using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Content.Dtos
{
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    // Root object often has a "spell" array in 5eTools
    public class CompendiumDto
    {
        [JsonPropertyName("spell")]
        public List<SpellDto> Spell { get; set; } = new();
    }

    public class SpellDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("school")]
        public string? School { get; set; }

        [JsonPropertyName("time")]
        public List<TimeDto> Time { get; set; } = new();

        [JsonPropertyName("range")]
        public RangeDto? Range { get; set; }

        [JsonPropertyName("components")]
        public ComponentsDto? Components { get; set; }

        [JsonPropertyName("duration")]
        public List<DurationDto> Duration { get; set; } = new();

        [JsonPropertyName("entries")]
        public List<object> Entries { get; set; } = new(); // Entries can be strings or objects

        [JsonPropertyName("spellAttack")]
        public List<string>? SpellAttack { get; set; }

        [JsonPropertyName("savingThrow")]
        public List<string>? SavingThrow { get; set; }

        [JsonPropertyName("damageInflict")]
        public List<string>? DamageInflict { get; set; }

        // damage is often [ ["8d6"] ] or [ ["1d10"], ["1d10"] ]
        // It's a list of lists of strings? Or objects?
        // Usually strings.
        [JsonPropertyName("damage")]
        public List<List<string>>? Damage { get; set; }
    }
#pragma warning restore CA2227
#pragma warning restore CA1002

    public class TimeDto
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
    }

    public class RangeDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("distance")]
        public DistanceDto? Distance { get; set; }
    }

    public class DistanceDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class ComponentsDto
    {
        [JsonPropertyName("v")]
        public bool V { get; set; }

        [JsonPropertyName("s")]
        public bool S { get; set; }

        [JsonPropertyName("m")]
        public object? M { get; set; } // Can be string or object
    }

    public class DurationDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("duration")]
        public DurationAmountDto? Duration { get; set; }

        [JsonPropertyName("concentration")]
        public bool Concentration { get; set; }
    }

    public class DurationAmountDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
