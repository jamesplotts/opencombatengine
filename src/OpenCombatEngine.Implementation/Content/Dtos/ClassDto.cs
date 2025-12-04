using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Content.Dtos
{
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    public class ClassCompendiumDto
    {
        [JsonPropertyName("class")]
        public List<ClassDto> Class { get; set; } = new();
    }

    public class ClassDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("hd")]
        public HdDto? Hd { get; set; }

        [JsonPropertyName("proficiency")]
        public List<string>? Proficiency { get; set; }

        // Simplified for now, just capturing the idea that features exist
        // In 5e.tools, features are linked. Here we might expect embedded or just basic parsing.
        // We'll add a placeholder for features if we can find a good way, 
        // but the plan mentioned ClassTableGroups.
        [JsonPropertyName("classTableGroups")]
        public List<ClassTableGroupDto>? ClassTableGroups { get; set; }
    }

    public class HdDto
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("faces")]
        public int Faces { get; set; }
    }

    public class ClassTableGroupDto
    {
        [JsonPropertyName("colLabels")]
        public List<string>? ColLabels { get; set; }

        [JsonPropertyName("rows")]
        public List<List<object>>? Rows { get; set; }
    }
#pragma warning restore CA2227
#pragma warning restore CA1002
}
