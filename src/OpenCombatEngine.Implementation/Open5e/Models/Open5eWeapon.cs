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
        
        // A get-only IList<T> property is NOT reliably populated by
        // System.Text.Json from a real HTTP response — the same defect
        // class already fixed on Open5eListResult<T>.Results (see that
        // type's own remarks), one level deeper: Results itself now
        // deserializes correctly, but each individual weapon inside it
        // still silently kept an empty Properties list against the real
        // Open5e API, with no exception thrown. Every weapon-property-
        // dependent feature (IWeapon.Range parsing, Thrown/Versatile/
        // TwoHanded/Ammunition detection, and everything built on top —
        // Attack's weapon-kind gating, Grapple's free-hand check) was
        // silently operating on an empty properties list for every
        // weapon fetched live from Open5e. Found via a cache round-trip
        // test (Open5eItemCache), not live traffic — same "no exception
        // thrown" silent-failure shape as the Results bug, still
        // undetected because every prior weapon test constructs
        // Open5eWeapon directly in C# rather than deserializing a real
        // response. A settable property lets the deserializer assign a
        // new list directly instead of relying on add-into-existing-
        // instance semantics.
        [JsonPropertyName("properties")]
#pragma warning disable CA2227 // settable is required for System.Text.Json to populate this from a real response — see remarks above
        public System.Collections.Generic.IList<string> Properties { get; set; } = new System.Collections.Generic.List<string>();
#pragma warning restore CA2227
    }
}
