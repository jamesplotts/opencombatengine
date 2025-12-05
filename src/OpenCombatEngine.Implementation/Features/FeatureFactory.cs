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

            if (description.Contains("As a bonus action", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("use a bonus action", StringComparison.OrdinalIgnoreCase))
            {
                var action = new OpenCombatEngine.Implementation.Actions.TextAction(name, description, ActionType.BonusAction);
                return new ActionFeature(name, action);
            }

            // Check for Spells
            // "You know the [spell] cantrip"
            // "You can cast [spell]"
            if (_spellRepository != null)
            {
                var spells = new System.Collections.Generic.List<OpenCombatEngine.Core.Interfaces.Spells.ISpell>();
                
                // Simple regex to find spell names might be hard without a list of all spells.
                // Instead, we can iterate known spells in repo and check if description contains them?
                // Or rely on specific phrasing.
                // Let's try iterating the repository if it's not too large, or just check for common patterns.
                // For now, let's assume the repository has a GetAll method or similar, but ISpellRepository usually has GetSpell(name).
                // Let's try to extract potential spell names from quotes or specific phrases.
                
                // Pattern: "You know the (.*?) cantrip"
                var cantripMatch = Regex.Match(description, @"You know the (.*?) cantrip", RegexOptions.IgnoreCase);
                if (cantripMatch.Success)
                {
                    var spellName = cantripMatch.Groups[1].Value;
                    var spellResult = _spellRepository.GetSpell(spellName);
                    if (spellResult.IsSuccess) spells.Add(spellResult.Value);
                }

                // Pattern: "You can cast (.*?)"
                // This is risky as it might match too much.
                // Let's stick to specific known patterns or exact matches if possible.
                
                if (spells.Count > 0)
                {
                    return new SpellcastingFeature(name, spells);
                }
            }

            // Check for Proficiencies
            // "Proficiency in Stealth"
            // "Proficiency in Dexterity saving throws"
            if (description.Contains("Proficiency in", StringComparison.OrdinalIgnoreCase))
            {
                // Try to extract skill or save
                // Regex: Proficiency in (.*?) (skill|saving throws)
                // Or simpler: just check common skills/saves
                
                // Check for saving throws first
                foreach (var ability in Enum.GetValues<Ability>())
                {
                    if (description.Contains($"{ability} saving throws", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ProficiencyFeature(name, ability);
                    }
                }

                // Check for skills (simplified list for now, or just extract word)
                // Let's extract the word after "Proficiency in "
                var match = Regex.Match(description, @"Proficiency in ([\w\s]+?)( skill|\.|$)");
                if (match.Success)
                {
                    var skill = match.Groups[1].Value.Trim();
                    // Basic validation to ensure it's not "saving throws" if regex matched weirdly
                    if (!skill.Contains("saving throws", StringComparison.OrdinalIgnoreCase))
                    {
                        return new ProficiencyFeature(name, skill);
                    }
                }
            }

            // Fallback to TextFeature
            return new TextFeature(name, description);
        }

        private static OpenCombatEngine.Core.Interfaces.Spells.ISpellRepository? _spellRepository;

        public static void SetSpellRepository(OpenCombatEngine.Core.Interfaces.Spells.ISpellRepository repository)
        {
            _spellRepository = repository;
        }
    }
}
