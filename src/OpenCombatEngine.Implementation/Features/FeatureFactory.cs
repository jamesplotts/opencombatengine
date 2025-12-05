using System;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Features;

namespace OpenCombatEngine.Implementation.Features
{
    public static class FeatureFactory
    {
        public static IFeature? CreateFeature(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (description == null) description = "";

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

            // Check for Damage Affinities
            // "Resistance to Fire", "Fire Resistance"
            // "Immunity to Poison", "Poison Immunity"
            // "Vulnerability to Cold", "Cold Vulnerability"
            
            // Helper to parse damage type
            DamageType? ParseDamageType(string text)
            {
                foreach (var type in Enum.GetValues<DamageType>())
                {
                    if (text.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return type;
                    }
                }
                return null;
            }

            if (name.Contains("Resistance", StringComparison.OrdinalIgnoreCase))
            {
                var type = ParseDamageType(name);
                if (type.HasValue)
                {
                    return new DamageAffinityFeature(name, type.Value, AffinityType.Resistance);
                }
            }

            if (name.Contains("Immunity", StringComparison.OrdinalIgnoreCase) || name.Contains("Immune", StringComparison.OrdinalIgnoreCase))
            {
                var type = ParseDamageType(name);
                if (type.HasValue)
                {
                    return new DamageAffinityFeature(name, type.Value, AffinityType.Immunity);
                }
            }

            if (name.Contains("Vulnerability", StringComparison.OrdinalIgnoreCase) || name.Contains("Vulnerable", StringComparison.OrdinalIgnoreCase))
            {
                var type = ParseDamageType(name);
                if (type.HasValue)
                {
                    return new DamageAffinityFeature(name, type.Value, AffinityType.Vulnerability);
                }
            }

            // Check for Actions
            // "As an action", "As a bonus action"
            if (description.Contains("As an action", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("use your action", StringComparison.OrdinalIgnoreCase))
            {
                var action = new OpenCombatEngine.Implementation.Actions.TextAction(name, description, ActionType.Action);
                return new ActionFeature(name, action);
            }

            if (description.Contains("As a bonus action", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("use a bonus action", StringComparison.OrdinalIgnoreCase))
            {
                var action = new OpenCombatEngine.Implementation.Actions.TextAction(name, description, ActionType.BonusAction);
                return new ActionFeature(name, action);
            }

            // Fallback to TextFeature
            return new TextFeature(name, description);
        }
    }
}
