using System;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Interfaces.Features;

namespace OpenCombatEngine.Implementation.Features
{
    public static class FeatureFactory
    {
        public static IFeature? CreateFeature(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            // Check for Darkvision
            if (name.Contains("Darkvision", StringComparison.OrdinalIgnoreCase))
            {
                // Try to parse range from description or name
                // e.g. "Darkvision 60 ft."
                var match = Regex.Match(description + " " + name, @"(\d+)\s*ft");
                int range = 60;
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int parsedRange))
                    {
                        range = parsedRange;
                    }
                }
                return new SenseFeature(name, "Darkvision", range);
            }

            // Check for Speed Increase
            // e.g. "Speed +10" or "Speed Increase" with desc "Your speed increases by 10 feet."
            if (name.Contains("Speed", StringComparison.OrdinalIgnoreCase))
            {
                // Try to find a number in the description
                var match = Regex.Match(description + " " + name, @"increases by (\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int bonus))
                {
                    return new AttributeBonusFeature(name, "Speed", bonus);
                }
                
                // Fallback for simple "Speed +10"
                match = Regex.Match(name, @"Speed \+(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out bonus))
                {
                    return new AttributeBonusFeature(name, "Speed", bonus);
                }
            }

            // Fallback to TextFeature
            return new TextFeature(name, description);
        }
    }
}
