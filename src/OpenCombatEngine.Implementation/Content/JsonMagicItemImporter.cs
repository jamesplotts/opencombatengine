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

            IWeapon? weaponProps = null;
            IArmor? armorProps = null;

            // Determine if it's a weapon
            if (dto.Type == "M" || dto.Type == "R") // Melee or Ranged
            {
                if (!string.IsNullOrWhiteSpace(dto.Dmg1))
                {
                    // Damage Type Mapping
                    OpenCombatEngine.Core.Enums.DamageType dmgTypeEnum = OpenCombatEngine.Core.Enums.DamageType.Slashing; // Default
                    if (!string.IsNullOrWhiteSpace(dto.DmgType))
                    {
                         switch(dto.DmgType.ToUpperInvariant())
                         {
                             case "P": dmgTypeEnum = OpenCombatEngine.Core.Enums.DamageType.Piercing; break;
                             case "B": dmgTypeEnum = OpenCombatEngine.Core.Enums.DamageType.Bludgeoning; break;
                             case "S": dmgTypeEnum = OpenCombatEngine.Core.Enums.DamageType.Slashing; break;
                         }
                    }
                    
                    weaponProps = new OpenCombatEngine.Implementation.Items.Weapon(
                        dto.Name ?? "Weapon",
                        dto.Dmg1,
                        dmgTypeEnum,
                        new List<OpenCombatEngine.Core.Enums.WeaponProperty>(), // properties
                        description,
                        weight,
                        (int)value
                    );
                }
            }
            
            // Determine if it's armor
            if (dto.Type == "HA" || dto.Type == "MA" || dto.Type == "LA" || dto.Type == "S")
            {
                if (dto.Ac.HasValue)
                {
                    OpenCombatEngine.Core.Interfaces.Items.ArmorCategory armorCategory = OpenCombatEngine.Core.Interfaces.Items.ArmorCategory.Light;
                    if (dto.Type == "HA") armorCategory = OpenCombatEngine.Core.Interfaces.Items.ArmorCategory.Heavy;
                    else if (dto.Type == "MA") armorCategory = OpenCombatEngine.Core.Interfaces.Items.ArmorCategory.Medium;
                    else if (dto.Type == "S") armorCategory = OpenCombatEngine.Core.Interfaces.Items.ArmorCategory.Shield;
                    
                    int strReq = 0;
                    if (!string.IsNullOrWhiteSpace(dto.Strength)) _ = int.TryParse(dto.Strength, out strReq);
                    
                    armorProps = new OpenCombatEngine.Implementation.Items.Armor(
                        dto.Name ?? "Armor",
                        dto.Ac.Value,
                        armorCategory,
                        null, // dex cap
                        strReq,
                        dto.Stealth == true,
                        description,
                        weight,
                        (int)value
                    );
                }
            }

            // Determine if it's a container
            IContainer? containerProps = null;
            if (dto.Name != null)
            {
                var lowerName = dto.Name; // Using OrdinalIgnoreCase below
                if (dto.Name.Contains("Bag of Holding", StringComparison.OrdinalIgnoreCase))
                {
                    // Bag of Holding: 15 lbs base, 500 lbs capacity, 0 weight multiplier (contents don't add weight)
                    containerProps = new OpenCombatEngine.Implementation.Items.ContainerItem(
                        dto.Name,
                        15,
                        500,
                        0.0,
                        description,
                        (int)value
                    );
                }
                else if (dto.Name.Contains("Handy Haversack", StringComparison.OrdinalIgnoreCase))
                {
                    // Heward's Handy Haversack: 5 lbs base, ~120 lbs capacity (simplified), 0 weight multiplier
                    containerProps = new OpenCombatEngine.Implementation.Items.ContainerItem(
                        dto.Name,
                        5,
                        120,
                        0.0,
                        description,
                        (int)value
                    );
                }
            }

            // Infer Default Slot
            OpenCombatEngine.Core.Enums.EquipmentSlot? defaultSlot = null;
            if (dto.Type == "RG") defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Ring1; // Or Ring2, generic Ring
            else if (dto.Type == "M" || dto.Type == "R") defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand;
            else if (dto.Type == "HA" || dto.Type == "MA" || dto.Type == "LA") defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Armor;
            else if (dto.Type == "S") defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand;
            else if (dto.Name != null)
            {
                var lowerName = dto.Name; // No need to lower if we use OrdinalIgnoreCase, but wait, Contains(string, StringComparison) is standard in .NET?
                // Actually, .NET Standard 2.0 / .NET Framework might not have Contains(string, StringComparison).
                // But we are likely on .NET 6/8.
                // Let's assume we can use it.
                // If not, we use IndexOf >= 0.
                
                if (dto.Name.Contains("boots", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("shoes", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("slippers", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Feet;
                else if (dto.Name.Contains("cloak", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("cape", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("mantle", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Shoulders;
                else if (dto.Name.Contains("amulet", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("necklace", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("periapt", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("scarf", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Neck;
                else if (dto.Name.Contains("helm", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("hat", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("cap", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("circlet", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("crown", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Head;
                else if (dto.Name.Contains("gloves", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("gauntlets", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("bracers", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Hands;
                else if (dto.Name.Contains("belt", StringComparison.OrdinalIgnoreCase) || dto.Name.Contains("girdle", StringComparison.OrdinalIgnoreCase)) defaultSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Waist;
            }

            var (rechargeFreq, rechargeFormula) = ParseRecharge(dto.Recharge);

            return new MagicItem(
                dto.Name ?? "Unknown Item",
                description,
                weight,
                (int)value,
                type,
                reqAttunement,
                features,
                null, // conditions
                dto.Charges ?? 0,
                dto.Recharge ?? "",
                rechargeFreq,
                rechargeFormula,
                weaponProps,
                armorProps,
                containerProps,
                defaultSlot
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
        private static (RechargeFrequency, string) ParseRecharge(string? recharge)
        {
            if (string.IsNullOrWhiteSpace(recharge)) return (RechargeFrequency.Unspecified, "");

            var upper = recharge.ToUpperInvariant();
            var frequency = RechargeFrequency.Other;

            if (upper.Contains("DAWN", StringComparison.Ordinal)) frequency = RechargeFrequency.Dawn;
            else if (upper.Contains("DUSK", StringComparison.Ordinal)) frequency = RechargeFrequency.Dusk;
            else if (upper.Contains("MIDNIGHT", StringComparison.Ordinal)) frequency = RechargeFrequency.Midnight;
            else if (upper.Contains("SHORT REST", StringComparison.Ordinal)) frequency = RechargeFrequency.ShortRest;
            else if (upper.Contains("LONG REST", StringComparison.Ordinal)) frequency = RechargeFrequency.LongRest;
            else if (upper.Contains("NEVER", StringComparison.Ordinal)) frequency = RechargeFrequency.Never;

            // Extract formula: "1d6+1" from "1d6+1 at dawn"
            // Regex for dice formula: \d+d\d+(\s*[\+\-]\s*\d+)?
            var match = System.Text.RegularExpressions.Regex.Match(upper, @"(\d+D\d+(\s*[\+\-]\s*\d+)?)");
            string formula = match.Success ? match.Value.Replace(" ", "", StringComparison.Ordinal) : "";

            // If no dice formula found, maybe it's a fixed number? "5 at dawn"
            if (string.IsNullOrEmpty(formula))
            {
                var numberMatch = System.Text.RegularExpressions.Regex.Match(upper, @"^(\d+)\s+AT");
                if (numberMatch.Success) formula = numberMatch.Groups[1].Value;
            }

            return (frequency, formula);
        }
    }
}
