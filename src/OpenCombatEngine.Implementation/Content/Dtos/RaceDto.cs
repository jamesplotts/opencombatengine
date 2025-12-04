using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Content.Dtos
{
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1002 // Do not expose generic lists
    public class RaceCompendiumDto
    {
        [JsonPropertyName("race")]
        public List<RaceDto> Race { get; set; } = new();
    }

    public class RaceDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        // Speed can be int or object in 5e.tools, usually object/int
        // We'll handle simple int or "walk" property if object.
        // For simplicity, let's assume it might be deserialized as object and we parse manually, 
        // or we define a SpeedDto that handles the common case.
        [JsonPropertyName("speed")]
        public object? Speed { get; set; } 

        [JsonPropertyName("size")]
        public object? Size { get; set; } // Can be string or list of strings

        [JsonPropertyName("ability")]
        public List<System.Text.Json.JsonElement>? Ability { get; set; }

        [JsonPropertyName("entries")]
        public List<object>? Entries { get; set; }
    }
#pragma warning restore CA2227
#pragma warning restore CA1002
}
