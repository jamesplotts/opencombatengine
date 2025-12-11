using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Open5e.Models;

namespace OpenCombatEngine.Implementation.Open5e
{
    public static class Open5eAdapter
    {
        public static SpellDto ToStandard(Open5eSpell source)
        {
            System.ArgumentNullException.ThrowIfNull(source);

            var dto = new SpellDto
            {
                Name = source.Name,
                Level = source.LevelInt,
                School = source.School?.Length > 0 ? source.School.Substring(0, 1) : "V", 
                Time = new List<TimeDto> { new TimeDto { Number = ParseNumber(source.CastingTime), Unit = ParseUnit(source.CastingTime) } },
                Range = new RangeDto { Distance = new DistanceDto { Amount = ParseNumber(source.Range), Type = ParseUnit(source.Range) } },
                Components = ParseComponents(source.Components, source.Material),
                Duration = new List<DurationDto> { new DurationDto { Type = source.Duration, Concentration = string.Equals(source.Concentration, "yes", System.StringComparison.OrdinalIgnoreCase) } },
                Entries = new List<object> { source.Desc, source.HigherLevel }.Where(x => !string.IsNullOrEmpty(x as string)).ToList()
            };

            dto.School = source.School?.ToUpperInvariant() switch
            {
                "ABJURATION" => "A",
                "CONJURATION" => "C",
                "DIVINATION" => "D",
                "ENCHANTMENT" => "E",
                "EVOCATION" => "V",
                "ILLUSION" => "I",
                "NECROMANCY" => "N",
                "TRANSMUTATION" => "T",
                _ => "V"
            };

            return dto;
        }

        private static int ParseNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            var parts = input.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts[0], out int val)) return val;
            return 0;
        }

        private static string ParseUnit(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var parts = input.Split(' ');
            if (parts.Length > 1) return parts[1];
            return input;
        }

        private static ComponentsDto ParseComponents(string components, string material)
        {
            var dto = new ComponentsDto();
            if (string.IsNullOrWhiteSpace(components)) return dto;

            if (components.Contains('V', System.StringComparison.OrdinalIgnoreCase)) dto.V = true;
            if (components.Contains('S', System.StringComparison.OrdinalIgnoreCase)) dto.S = true;
            if (components.Contains('M', System.StringComparison.OrdinalIgnoreCase)) dto.M = material;
            
            return dto;
        }

        public static MonsterDto ToStandard(Open5eMonster source)
        {
            System.ArgumentNullException.ThrowIfNull(source);

            var dto = new MonsterDto
            {
                Name = source.Name,
                Size = source.Size,
                Type = source.Type,
                Alignment = new List<string> { source.Alignment },
                Str = source.Strength,
                Dex = source.Dexterity,
                Con = source.Constitution,
                Intelligence = source.Intelligence,
                Wis = source.Wisdom,
                Cha = source.Charisma,
                
                Hp = new HpDto { Average = source.HitPoints, Formula = source.HitDice },
                Ac = new List<object> { source.ArmorClass }, 
            };
            
            if (source.Actions != null)
            {
                dto.Action = new List<MonsterActionDto>();
                foreach (var act in source.Actions)
                {
                    var newAct = new MonsterActionDto
                    {
                        Name = act.Name,
                        Entries = new List<object> { act.Desc ?? "" }
                    };
                    dto.Action.Add(newAct);
                    
                    if (act.AttackBonus.HasValue) newAct.Entries.Add($"{{@hit {act.AttackBonus.Value}}}");
                    if (!string.IsNullOrEmpty(act.DamageDice)) newAct.Entries.Add($"{{@damage {act.DamageDice}}}");
                }
            }

            return dto;
        }
    }
}
