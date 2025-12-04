using System.Collections.Generic;
using System.Text.Json;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Implementation.Features;

namespace OpenCombatEngine.Implementation.Content
{
    public static class FeatureParsingService
    {
        public static IEnumerable<IFeature> ParseFeatures(JsonElement element)
        {
            var features = new List<IFeature>();

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var feature = ParseSingleFeature(item);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                var feature = ParseSingleFeature(element);
                if (feature != null)
                {
                    features.Add(feature);
                }
            }

            return features;
        }

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        private static IFeature? ParseSingleFeature(JsonElement element)
#pragma warning restore CA1859
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            string name = "Unknown Feature";
            string description = "";

            if (element.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
            {
                name = nameProp.GetString() ?? name;
            }

            if (element.TryGetProperty("entries", out var entriesProp))
            {
                description = ParseEntries(entriesProp);
            }

            // Use factory to create specific feature or fallback to TextFeature
            return FeatureFactory.CreateFeature(name, description);
        }

        private static string ParseEntries(JsonElement entries)
        {
            if (entries.ValueKind == JsonValueKind.String)
            {
                return entries.GetString() ?? "";
            }
            else if (entries.ValueKind == JsonValueKind.Array)
            {
                var strings = new List<string>();
                foreach (var item in entries.EnumerateArray())
                {
                    strings.Add(ParseEntries(item));
                }
                return string.Join("\n", strings);
            }
            else if (entries.ValueKind == JsonValueKind.Object)
            {
                // Handle complex entries (e.g. lists, tables) - simplified for now
                // Often has "type": "list" or "entries" property
                if (entries.TryGetProperty("entries", out var subEntries))
                {
                    return ParseEntries(subEntries);
                }
            }

            return "";
        }
    }
}
