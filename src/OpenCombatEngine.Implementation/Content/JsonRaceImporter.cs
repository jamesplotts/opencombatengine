using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Races;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Races;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonRaceImporter : IContentImporter<IRaceDefinition>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Result<IEnumerable<IRaceDefinition>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return Result<IEnumerable<IRaceDefinition>>.Success(Enumerable.Empty<IRaceDefinition>());

            try
            {
                var compendium = JsonSerializer.Deserialize<RaceCompendiumDto>(data, _options);
                if (compendium?.Race == null) return Result<IEnumerable<IRaceDefinition>>.Success(Enumerable.Empty<IRaceDefinition>());

                var races = compendium.Race.Select(MapDtoToRace).Where(r => r != null).Cast<IRaceDefinition>().ToList();
                return Result<IEnumerable<IRaceDefinition>>.Success(races);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<IRaceDefinition>>.Failure($"JSON parsing error: {ex.Message}");
            }
        }

        private static IRaceDefinition? MapDtoToRace(RaceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return null;

            int speed = ParseSpeed(dto.Speed);
            Size size = ParseSize(dto.Size);
            var asi = ParseAbilityScoreIncreases(dto.Ability);

            // TODO: Parse racial features from Entries
            
            return new RaceDefinition(dto.Name, speed, size, asi);
        }

        private static int ParseSpeed(object? speedObj)
        {
            if (speedObj == null) return 30;

            if (speedObj is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number)
                {
                    return element.GetInt32();
                }
                else if (element.ValueKind == JsonValueKind.Object)
                {
                    // Try to get "walk" property
                    if (element.TryGetProperty("walk", out var walkProp) && walkProp.ValueKind == JsonValueKind.Number)
                    {
                        return walkProp.GetInt32();
                    }
                }
            }
            // Fallback
            return 30;
        }

        private static Size ParseSize(object? sizeObj)
        {
            if (sizeObj == null) return Size.Medium;

            string sizeStr = "M";

            if (sizeObj is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    sizeStr = element.GetString() ?? "M";
                }
                else if (element.ValueKind == JsonValueKind.Array)
                {
                    // Take the first one?
                    if (element.GetArrayLength() > 0)
                    {
                        var first = element[0];
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            sizeStr = first.GetString() ?? "M";
                        }
                    }
                }
            }

            return sizeStr switch
            {
                "T" => Size.Tiny,
                "S" => Size.Small,
                "M" => Size.Medium,
                "L" => Size.Large,
                "H" => Size.Huge,
                "G" => Size.Gargantuan,
                _ => Size.Medium
            };
        }

        private static Dictionary<Ability, int> ParseAbilityScoreIncreases(List<JsonElement>? abilityList)
        {
            var result = new Dictionary<Ability, int>();
            if (abilityList == null) return result;

            foreach (var element in abilityList)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        if (TryGetAbility(property.Name, out var ability))
                        {
                            if (property.Value.ValueKind == JsonValueKind.Number)
                            {
                                int val = property.Value.GetInt32();
                                if (result.ContainsKey(ability))
                                {
                                    result[ability] += val;
                                }
                                else
                                {
                                    result[ability] = val;
                                }
                            }
                        }
                        // Ignore "choose" or other non-ability keys for now
                    }
                }
            }
            return result;
        }

        private static bool TryGetAbility(string key, out Ability ability)
        {
            // Try standard parse first
            if (Enum.TryParse(key, true, out ability)) return true;

            // Try abbreviations
            switch (key.ToUpperInvariant())
            {
                case "STR": ability = Ability.Strength; return true;
                case "DEX": ability = Ability.Dexterity; return true;
                case "CON": ability = Ability.Constitution; return true;
                case "INT": ability = Ability.Intelligence; return true;
                case "WIS": ability = Ability.Wisdom; return true;
                case "CHA": ability = Ability.Charisma; return true;
                default: ability = default; return false;
            }
        }
    }
}
