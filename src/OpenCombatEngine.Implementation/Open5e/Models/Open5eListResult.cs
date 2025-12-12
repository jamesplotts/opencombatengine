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
        
        [JsonPropertyName("results")]
        public System.Collections.Generic.IList<T> Results { get; } = new System.Collections.Generic.List<T>();
    }
}
