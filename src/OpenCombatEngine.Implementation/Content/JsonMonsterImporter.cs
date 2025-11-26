using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Dice;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonMonsterImporter : IContentImporter<ICreature>
    {
        public Result<IEnumerable<ICreature>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Result<IEnumerable<ICreature>>.Failure("Data cannot be empty.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var doc = JsonDocument.Parse(data);
                IEnumerable<MonsterDto>? monsterDtos = null;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    monsterDtos = JsonSerializer.Deserialize<List<MonsterDto>>(data, options);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("monster", out _))
                    {
                        var compendium = JsonSerializer.Deserialize<MonsterCompendiumDto>(data, options);
                        monsterDtos = compendium?.Monster;
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<MonsterDto>(data, options);
                        if (single != null) monsterDtos = new List<MonsterDto> { single };
                    }
                }

                if (monsterDtos == null || !monsterDtos.Any())
                {
                    return Result<IEnumerable<ICreature>>.Success(Enumerable.Empty<ICreature>());
                }

                var creatures = new List<ICreature>();
                foreach (var dto in monsterDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                    creatures.Add(MapDtoToCreature(dto));
                }

                return Result<IEnumerable<ICreature>>.Success(creatures);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<ICreature>>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<IEnumerable<ICreature>>.Failure($"Import error: {ex.Message}");
            }
        }

        private static StandardCreature MapDtoToCreature(MonsterDto dto)
        {
            // 1. Ability Scores
            var abilities = new StandardAbilityScores(
                dto.Str, dto.Dex, dto.Con, dto.Intelligence, dto.Wis, dto.Cha
            );

            // 2. Hit Points
            // Use average if available, otherwise calculate from formula or default
            var maxHp = dto.Hp?.Average ?? 10;
            var hitPoints = new StandardHitPoints(maxHp);

            // 3. Create Creature
            var inventory = new StandardInventory();
            var turnManager = new StandardTurnManager(new StandardDiceRoller());
            
            var creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                dto.Name ?? "Unknown Monster",
                abilities,
                hitPoints,
                inventory,
                turnManager
            );
            
            // Fix circular dependency manually if needed
            // if (conditions is StandardConditionManager scm) scm.Creature = creature; // If property exists
            // Let's assume it's fine for now or I'll get a build error if I try to access property.

            // 4. AC
            if (dto.Ac != null && dto.Ac.Count > 0)
            {
                var firstAc = dto.Ac.First();
                if (firstAc is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                {
                    var targetAc = elem.GetInt32();
                    var dexMod = abilities.GetModifier(Ability.Dexterity);
                    var neededBonus = targetAc - 10 - dexMod;
                    
                    if (neededBonus > 0)
                    {
                        // TODO: Implement Natural Armor support.
                    }
                }
            }

            // 5. Actions
            if (dto.Action != null)
            {
                foreach (var actionDto in dto.Action)
                {
                    if (string.IsNullOrWhiteSpace(actionDto.Name)) continue;
                    
                    var entriesStr = string.Join(" ", actionDto.Entries?.Select(e => e.ToString()) ?? Array.Empty<string>());
                    
                    var toHit = 0;
                    var damageDice = "1d4"; // Default
                    var damageType = DamageType.Bludgeoning; // Default

                    // Regex for hit: {@hit <number>}
                    var hitMatch = Regex.Match(entriesStr, @"\{@hit ([+-]?\d+)\}");
                    if (hitMatch.Success)
                    {
                        _ = int.TryParse(hitMatch.Groups[1].Value, out toHit);
                    }

                    // Regex for damage: {@damage <dice>}
                    var damageMatch = Regex.Match(entriesStr, @"\{@damage ([^}]+)\}");
                    if (damageMatch.Success)
                    {
                        damageDice = damageMatch.Groups[1].Value;
                    }

                    // Simple damage type guess
                    if (entriesStr.Contains("slashing", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Slashing;
                    else if (entriesStr.Contains("piercing", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Piercing;
                    else if (entriesStr.Contains("fire", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Fire;

                    var action = new MonsterAttackAction(
                        actionDto.Name,
                        entriesStr, // Raw description
                        toHit,
                        damageDice,
                        damageType
                    );

                    creature.AddAction(action);
                }
            }

            return creature;
        }
    }
}
