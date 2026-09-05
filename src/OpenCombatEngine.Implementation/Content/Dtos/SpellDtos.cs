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

        // Not part of the 5etools format this DTO otherwise mirrors — an
        // Open5e-specific addition (Open5eAdapter populates this from
        // prose via Open5eSpellTextParser.ExtractHealingDice, since
        // Open5e's REST API has no structured healing field either).
        [JsonPropertyName("healingDice")]
        public string? HealingDice { get; set; }

        // Also Open5e-specific additions, populated the same way — see
        // Open5eSpellTextParser.ExtractInstanceCount/
        // ExtractInstanceCountPerUpcastLevel's doc comments.
        [JsonPropertyName("instanceCount")]
        public int InstanceCount { get; set; } = 1;

        [JsonPropertyName("instanceCountPerUpcastLevel")]
        public int InstanceCountPerUpcastLevel { get; set; }

        // Also Open5e-specific — populated from its real dnd_class field
        // (Open5eAdapter splits the comma-separated string), not part of
        // the 5etools format this DTO otherwise mirrors. Used by
        // character-creation's spell-pick prompts (ICharacterCreationService)
        // to filter the real spell repository down to one class's actual
        // list, rather than a second, hand-authored spell list.
        [JsonPropertyName("classes")]
        public List<string> Classes { get; set; } = new();
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
