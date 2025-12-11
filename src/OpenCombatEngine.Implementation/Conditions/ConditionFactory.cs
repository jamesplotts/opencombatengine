using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Conditions
{
    public static class ConditionFactory
    {
        public static OpenCombatEngine.Implementation.Conditions.Condition? Create(string name, string duration, ICreature? target = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            int durationRounds = ParseDuration(duration);
            ConditionType type = ParseConditionType(name);
            string description = $"Condition {name} applied via spell/effect.";

            // In the future, we can switch on Name/Type to return specific subclasses 
            // if we implement complex logic for specific conditions (e.g. Grappled might need a grappler source).
            // For now, generic Condition class is sufficient as it holds the Type enum which the engine checks.
            
            return new OpenCombatEngine.Implementation.Conditions.Condition(name, description, durationRounds, type);
        }

        public static OpenCombatEngine.Implementation.Conditions.Condition? Create(ConditionType type, int durationRounds, ICreature? target = null)
        {
            if (type == ConditionType.None || type == ConditionType.Unspecified) return null;
            
            string name = type.ToString();
            string description = $"Condition {name} restored/applied.";
            
            return new OpenCombatEngine.Implementation.Conditions.Condition(name, description, durationRounds, type);
        }

        public static OpenCombatEngine.Implementation.Conditions.Condition? Create(ConditionType type, ICreature? target = null)
        {
            return Create(type, 10, target);
        }

        private static int ParseDuration(string duration)
        {
            if (string.IsNullOrWhiteSpace(duration)) return 0; // Instantaneous or unknown

            // Simple heuristics for 5e durations
            if (duration.Contains("instant", StringComparison.OrdinalIgnoreCase)) return 0;
            
            int multiplier = 1;
            // Parse number if present at start "1 minute", "10 minutes"
            var parts = duration.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts[0], out int val))
            {
                multiplier = val;
            }

            if (duration.Contains("minute", StringComparison.OrdinalIgnoreCase)) return 10 * multiplier;
            if (duration.Contains("hour", StringComparison.OrdinalIgnoreCase)) return 600 * multiplier;
            if (duration.Contains("day", StringComparison.OrdinalIgnoreCase)) return 14400 * multiplier; // 24 * 600
            if (duration.Contains("round", StringComparison.OrdinalIgnoreCase)) return 1 * multiplier;
            if (duration.Contains("turn", StringComparison.OrdinalIgnoreCase)) return 1; // Until end of next turn?

            return 0; // Default
        }

        private static ConditionType ParseConditionType(string name)
        {
            if (Enum.TryParse<ConditionType>(name, true, out var type))
            {
                return type;
            }
            return ConditionType.Custom;
        }
    }
}
