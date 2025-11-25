using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonSpellImporter : IContentImporter<ISpell>
    {
        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller;

        public JsonSpellImporter(OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller diceRoller)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public Result<IEnumerable<ISpell>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Result<IEnumerable<ISpell>>.Failure("Data cannot be empty.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                using var doc = JsonDocument.Parse(data);
                IEnumerable<SpellDto>? spellDtos = null;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    spellDtos = JsonSerializer.Deserialize<List<SpellDto>>(data, options);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("spell", out _))
                    {
                        var compendium = JsonSerializer.Deserialize<CompendiumDto>(data, options);
                        spellDtos = compendium?.Spell;
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<SpellDto>(data, options);
                        if (single != null) spellDtos = new List<SpellDto> { single };
                    }
                }

                if (spellDtos == null || !spellDtos.Any())
                {
                    return Result<IEnumerable<ISpell>>.Success(Enumerable.Empty<ISpell>());
                }

                var spells = new List<ISpell>();
                foreach (var dto in spellDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                    var spell = MapDtoToSpell(dto);
                    spells.Add(spell);
                }

                return Result<IEnumerable<ISpell>>.Success(spells);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<ISpell>>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<IEnumerable<ISpell>>.Failure($"Import error: {ex.Message}");
            }
        }

        private Spell MapDtoToSpell(SpellDto dto)
        {
            var school = MapSchool(dto.School);
            var components = MapComponents(dto.Components);
            var time = MapTime(dto.Time);
            var range = MapRange(dto.Range);
            var duration = MapDuration(dto.Duration);
            var description = MapEntries(dto.Entries);

            var requiresAttack = dto.SpellAttack != null && dto.SpellAttack.Count > 0;
            var requiresConcentration = MapConcentration(dto.Duration);
            var saveAbility = MapSaveAbility(dto.SavingThrow);
            var damageType = MapDamageType(dto.DamageInflict);
            var damageDice = MapDamageDice(dto.Damage);

            return new Spell(
                dto.Name ?? "Unknown",
                dto.Level,
                school,
                time,
                range,
                components,
                duration,
                description,
                _diceRoller,
                requiresAttack,
                requiresConcentration,
                saveAbility,
                damageDice,
                damageType
            );
        }

        private static bool MapConcentration(List<DurationDto>? duration)
        {
            if (duration == null || duration.Count == 0) return false;
            // Check if any duration entry has concentration = true
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

        private static DamageType? MapDamageType(List<string>? types)
        {
            if (types == null || types.Count == 0) return null;
            var type = types[0].ToUpperInvariant();
            // Simple mapping, expand as needed
            return type switch
            {
                "ACID" => DamageType.Acid,
                "BLUDGEONING" => DamageType.Bludgeoning,
                "COLD" => DamageType.Cold,
                "FIRE" => DamageType.Fire,
                "FORCE" => DamageType.Force,
                "LIGHTNING" => DamageType.Lightning,
                "NECROTIC" => DamageType.Necrotic,
                "PIERCING" => DamageType.Piercing,
                "POISON" => DamageType.Poison,
                "PSYCHIC" => DamageType.Psychic,
                "RADIANT" => DamageType.Radiant,
                "SLASHING" => DamageType.Slashing,
                "THUNDER" => DamageType.Thunder,
                _ => null
            };
        }

        private static string? MapDamageDice(List<List<string>>? damage)
        {
            if (damage == null || damage.Count == 0) return null;
            // Take first damage entry
            var firstGroup = damage[0];
            if (firstGroup == null || firstGroup.Count == 0) return null;
            return firstGroup[0]; // e.g. "8d6"
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
