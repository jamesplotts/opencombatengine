using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Content.Mappers
{
    public static class MonsterMapper
    {
        public static StandardCreature Map(MonsterDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var abilities = new StandardAbilityScores(
                dto.Str, dto.Dex, dto.Con, dto.Intelligence, dto.Wis, dto.Cha
            );

            var maxHp = dto.Hp?.Average ?? 10;
            var hitPoints = new StandardHitPoints(maxHp);

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
            
            // AC Logic
            if (dto.Ac != null && dto.Ac.Count > 0)
            {
                var firstAc = dto.Ac.First();
                if (firstAc is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                {
                    // Basic AC logic placeholder
                }
            }

            // Actions
            if (dto.Action != null)
            {
                foreach (var actionDto in dto.Action)
                {
                    if (string.IsNullOrWhiteSpace(actionDto.Name)) continue;
                    
                    var entriesStr = string.Join(" ", actionDto.Entries?.Select(e => e.ToString()) ?? Array.Empty<string>());
                    
                    var toHit = 0;
                    var damageDice = "1d4"; // Default
                    var damageType = DamageType.Bludgeoning; // Default

                    var hitMatch = Regex.Match(entriesStr, @"\{@hit ([+-]?\d+)\}");
                    if (hitMatch.Success)
                    {
                        _ = int.TryParse(hitMatch.Groups[1].Value, out toHit);
                    }

                    var damageMatch = Regex.Match(entriesStr, @"\{@damage ([^}]+)\}");
                    if (damageMatch.Success)
                    {
                        damageDice = damageMatch.Groups[1].Value;
                    }

                    if (entriesStr.Contains("slashing", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Slashing;
                    else if (entriesStr.Contains("piercing", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Piercing;
                    else if (entriesStr.Contains("fire", StringComparison.OrdinalIgnoreCase)) damageType = DamageType.Fire;

                    var action = new MonsterAttackAction(
                        actionDto.Name,
                        entriesStr,
                        toHit,
                        damageDice,
                        damageType
                    );

                    creature.AddAction(action);
                }
            }
            if (dto.Tags != null)
            {
                creature.Tags = dto.Tags.ToList();
            }

            return creature;
        }
    }
}
