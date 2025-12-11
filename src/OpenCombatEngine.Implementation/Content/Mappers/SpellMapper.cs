using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.Implementation.Content.Mappers
{
    public static class SpellMapper
    {
        public static ISpell Map(SpellDto dto, IDiceRoller diceRoller)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(diceRoller);

            var school = MapSchool(dto.School);
            var components = MapComponents(dto.Components);
            var time = MapTime(dto.Time);
            var range = MapRange(dto.Range);
            var duration = MapDuration(dto.Duration);
            var description = MapEntries(dto.Entries);

            var requiresAttack = dto.SpellAttack != null && dto.SpellAttack.Count > 0;
            var requiresConcentration = MapConcentration(dto.Duration);
            var saveAbility = MapSaveAbility(dto.SavingThrow);
            var saveEffect = MapSaveEffect(dto.SavingThrow, description);
            
            var damageRolls = MapDamageRolls(dto.Damage, dto.DamageInflict);
            var healingDice = MapHealingDice(dto.Entries);

            return new Spell(
                dto.Name ?? "Unknown",
                dto.Level,
                school,
                time,
                range,
                components,
                duration,
                description,
                diceRoller,
                requiresAttack,
                requiresConcentration,
                saveAbility,
                saveEffect,
                damageRolls,
                healingDice
            );
        }

        private static OpenCombatEngine.Core.Enums.SaveEffect MapSaveEffect(List<string>? saves, string description)
        {
            if (saves == null || saves.Count == 0) return OpenCombatEngine.Core.Enums.SaveEffect.None;
            
            if (description.Contains("half as much damage", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("half damage", StringComparison.OrdinalIgnoreCase))
            {
                return OpenCombatEngine.Core.Enums.SaveEffect.HalfDamage;
            }
            
            return OpenCombatEngine.Core.Enums.SaveEffect.Negate;
        }

        private static List<OpenCombatEngine.Core.Models.Spells.DamageFormula> MapDamageRolls(List<List<string>>? damage, List<string>? damageInflict)
        {
            var list = new List<OpenCombatEngine.Core.Models.Spells.DamageFormula>();
            if (damage == null || damage.Count == 0) return list;

            for (int i = 0; i < damage.Count; i++)
            {
                var dice = damage[i].FirstOrDefault(); 
                if (string.IsNullOrWhiteSpace(dice)) continue;
                
                var typeStr = (damageInflict != null && damageInflict.Count > i) ? damageInflict[i] : (damageInflict?.FirstOrDefault() ?? "force");
                
                if (!Enum.TryParse<DamageType>(typeStr, true, out var type))
                {
                    type = DamageType.Force;
                }
                
                list.Add(new OpenCombatEngine.Core.Models.Spells.DamageFormula(dice, type));
            }
            
            return list;
        }

        private static string? MapHealingDice(List<object>? entries)
        {
            return null; 
        }

        private static bool MapConcentration(List<DurationDto>? duration)
        {
            if (duration == null || duration.Count == 0) return false;
            return duration.Any(d => d.Concentration);
        }

        private static Ability? MapSaveAbility(List<string>? saves)
        {
            if (saves == null || saves.Count == 0) return null;
            var save = saves[0].ToUpperInvariant();
            return save switch
            {
                "STR" => Ability.Strength,
                "DEX" => Ability.Dexterity,
                "CON" => Ability.Constitution,
                "INT" => Ability.Intelligence,
                "WIS" => Ability.Wisdom,
                "CHA" => Ability.Charisma,
                _ => null
            };
        }

        private static SpellSchool MapSchool(string? code)
        {
            return code?.ToUpperInvariant() switch
            {
                "A" => SpellSchool.Abjuration,
                "C" => SpellSchool.Conjuration,
                "D" => SpellSchool.Divination,
                "E" => SpellSchool.Enchantment,
                "V" => SpellSchool.Evocation,
                "I" => SpellSchool.Illusion,
                "N" => SpellSchool.Necromancy,
                "T" => SpellSchool.Transmutation,
                _ => SpellSchool.Evocation
            };
        }

        private static string MapComponents(ComponentsDto? dto)
        {
            if (dto == null) return "";
            var parts = new List<string>();
            if (dto.V) parts.Add("V");
            if (dto.S) parts.Add("S");
            if (dto.M != null)
            {
                if (dto.M is JsonElement elem && elem.ValueKind == JsonValueKind.String)
                {
                    parts.Add($"M ({elem.GetString()})");
                }
                else if (dto.M is string str)
                {
                     parts.Add($"M ({str})");
                }
                else
                {
                    parts.Add("M");
                }
            }
            return string.Join(", ", parts);
        }

        private static string MapTime(List<TimeDto>? time)
        {
            if (time == null || time.Count == 0) return "";
            var first = time[0];
            return $"{first.Number} {first.Unit}";
        }

        private static string MapRange(RangeDto? range)
        {
            if (range == null) return "";
            if (range.Distance != null)
            {
                return $"{range.Distance.Amount} {range.Distance.Type}";
            }
            return range.Type ?? "";
        }

        private static string MapDuration(List<DurationDto>? duration)
        {
            if (duration == null || duration.Count == 0) return "";
            var first = duration[0];
            if (first.Type == "instant") return "Instantaneous";
            if (first.Duration != null)
            {
                return $"{first.Duration.Amount} {first.Duration.Type}";
            }
            return first.Type ?? "";
        }

        private static string MapEntries(List<object>? entries)
        {
            if (entries == null) return "";
            var lines = new List<string>();
            foreach (var entry in entries)
            {
                if (entry is JsonElement elem && elem.ValueKind == JsonValueKind.String)
                {
                    lines.Add(elem.GetString() ?? "");
                }
                else if (entry is string str)
                {
                    lines.Add(str);
                }
            }
            return string.Join("\n\n", lines);
        }
    }
}
