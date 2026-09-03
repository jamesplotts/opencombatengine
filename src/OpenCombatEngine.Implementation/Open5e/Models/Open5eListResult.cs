using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenCombatEngine.Implementation.Open5e.Models
{
    public class Open5eListResult<T>
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("next")]
        public string? Next { get; set; }
        
        [JsonPropertyName("previous")]
        public string? Previous { get; set; }
        
        // A get-only IList<T> property is NOT reliably populated by
        // System.Text.Json from a real HTTP response — confirmed live: the
        // "count"/"next" scalar fields deserialized correctly while
        // "results" silently stayed empty against the real Open5e API,
        // with no exception thrown. A settable property lets the
        // deserializer assign a new list directly instead of relying on
        // add-into-existing-instance semantics. This had gone unnoticed
        // because every prior test of this type (weapons/armor/magic
        // items) constructs the object directly in C# via mocks — this
        // repo's first test to deserialize a real Open5e list response
        // (Open5eSpellListTests / Open5eIntegrationTests, added for the
        // spell repository) is what surfaced it.
        [JsonPropertyName("results")]
#pragma warning disable CA2227 // settable is required for System.Text.Json to populate this from a real response — see remarks above
        public System.Collections.Generic.IList<T> Results { get; set; } = new System.Collections.Generic.List<T>();
#pragma warning restore CA2227
    }
}
