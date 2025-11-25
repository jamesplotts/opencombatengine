using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Content
{
    public class JsonMagicItemImporter : IContentImporter<IMagicItem>
    {
        public Result<IEnumerable<IMagicItem>> Import(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return Result<IEnumerable<IMagicItem>>.Failure("Data cannot be empty.");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var doc = JsonDocument.Parse(data);
                IEnumerable<MagicItemDto>? itemDtos = null;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    itemDtos = JsonSerializer.Deserialize<List<MagicItemDto>>(data, options);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("item", out _))
                    {
                        var compendium = JsonSerializer.Deserialize<MagicItemCompendiumDto>(data, options);
                        itemDtos = compendium?.Item;
                    }
                    else
                    {
                        var single = JsonSerializer.Deserialize<MagicItemDto>(data, options);
                        if (single != null) itemDtos = new List<MagicItemDto> { single };
                    }
                }

                if (itemDtos == null || !itemDtos.Any())
                {
                    return Result<IEnumerable<IMagicItem>>.Success(Enumerable.Empty<IMagicItem>());
                }

                var items = new List<IMagicItem>();
                foreach (var dto in itemDtos)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                    items.Add(MapDtoToItem(dto));
                }

                return Result<IEnumerable<IMagicItem>>.Success(items);
            }
            catch (JsonException ex)
            {
                return Result<IEnumerable<IMagicItem>>.Failure($"JSON parsing error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return Result<IEnumerable<IMagicItem>>.Failure($"Import error: {ex.Message}");
            }
        }

        private static MagicItem MapDtoToItem(MagicItemDto dto)
        {
            var type = ParseItemType(dto.Type);
            var reqAttunement = ParseAttunement(dto.ReqAttune);
            var description = ParseDescription(dto.Entries);
            var weight = dto.Weight ?? 0;
            var value = dto.Value ?? 0; // Simplified value handling

            var features = new List<IFeature>();
            var bonuses = new Dictionary<StatType, int>();

            // AC Bonus
            if (!string.IsNullOrWhiteSpace(dto.BonusAc))
            {
                if (int.TryParse(dto.BonusAc, out int acBonus))
                {
                    bonuses[StatType.ArmorClass] = acBonus;
                }
            }

            // Weapon Bonus (Attack and Damage)
            if (!string.IsNullOrWhiteSpace(dto.BonusWeapon))
            {
                if (int.TryParse(dto.BonusWeapon, out int weaponBonus))
                {
                    bonuses[StatType.AttackRoll] = weaponBonus;
                    bonuses[StatType.DamageRoll] = weaponBonus;
                }
            }

            // Saving Throw Bonus
            if (!string.IsNullOrWhiteSpace(dto.BonusSavingThrow))
            {
                 if (int.TryParse(dto.BonusSavingThrow, out int saveBonus))
                {
                    bonuses[StatType.SavingThrow] = saveBonus;
                }
            }

            if (bonuses.Count > 0)
            {
                features.Add(new OpenCombatEngine.Implementation.Features.StatBonusFeature(
                    $"Bonuses from {dto.Name}",
                    "Passive bonuses",
                    bonuses
                ));
            }

            return new MagicItem(
                dto.Name ?? "Unknown Item",
                description,
                weight,
                (int)value, // Casting long to int for now, might need to update IItem
                type,
                reqAttunement,
                features,
                null, // conditions
                dto.Charges ?? 0,
                dto.Recharge ?? ""
            );
        }

        private static ItemType ParseItemType(string? typeStr)
        {
            if (string.IsNullOrWhiteSpace(typeStr)) return ItemType.Other;
            
            // Map common 5eTools types to our enum
            // "W" = Wonderous, "R" = Ring, "A" = Armor, "M" = Melee Weapon, "R" = Ranged Weapon? No, "M" is melee weapon.
            // 5eTools types: "HA" (Heavy Armor), "MA" (Medium), "LA" (Light), "S" (Shield), "M" (Melee Weapon), "R" (Ranged Weapon), "RG" (Ring), "W" (Wondrous), "P" (Potion), "SC" (Scroll), "WD" (Wand), "RD" (Rod), "ST" (Staff)
            
            return typeStr.ToUpperInvariant() switch
            {
                "HA" or "MA" or "LA" or "S" => ItemType.Armor,
                "M" or "R" => ItemType.Weapon,
                "RG" => ItemType.Ring,
                "W" => ItemType.WondrousItem,
                "P" => ItemType.Potion,
                "SC" => ItemType.Scroll,
                "WD" => ItemType.Wand,
                "RD" => ItemType.Rod,
                "ST" => ItemType.Staff,
                _ => ItemType.Other
            };
        }

        private static bool ParseAttunement(object? reqAttune)
        {
            if (reqAttune == null) return false;
            
            if (reqAttune is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.True) return true;
                if (elem.ValueKind == JsonValueKind.False) return false;
                if (elem.ValueKind == JsonValueKind.String) return true; // "by a spellcaster" etc.
            }
            
            if (reqAttune is bool b) return b;
            if (reqAttune is string) return true;

            return false;
        }

        private static string ParseDescription(IEnumerable<object>? entries)
        {
            if (entries == null) return string.Empty;
            return string.Join("\n", entries.Select(e => e.ToString()));
        }
    }
}
