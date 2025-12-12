using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Open5e.Models;

namespace OpenCombatEngine.Implementation.Content.Mappers
{
    public static class Open5eItemMapper
    {
        public static IWeapon MapWeapon(Open5eWeapon source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var weapon = new StandardWeapon(
                Guid.NewGuid(),
                source.Name,
                $"Category: {source.Category}",
                ParseWeight(source.Weight),
                ParseCost(source.Cost),
                ItemRarity.Common, // Standard weapons are common
                source.DamageDice,
                ParseDamageType(source.DamageType),
                ParseWeaponProperties(source.Properties)
            );
            return weapon;
        }

        public static IArmor MapArmor(Open5eArmor source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var category = ParseArmorCategory(source.Category);
            // Deduce Dex Cap
            int? dexCap = category switch
            {
               ArmorCategory.Light => null, // No limit
               ArmorCategory.Medium => 2,
               ArmorCategory.Heavy => 0, // No dex
               ArmorCategory.Shield => 0, // Shields don't usually add dex to their bonus, but do they cap it? No.
               _ => null
            };

            // Override if data has better info? Open5e doesn't explicitly send "Dex Cap".
            // It sends "plus_dex_mod".
            // If !plus_dex_mod, Cap is effectively 0 (Heavy).
            if (!source.PlusDexMod) dexCap = 0;

            var armor = new StandardArmor(
                Guid.NewGuid(),
                source.Name,
                $"Category: {source.Category}",
                ParseWeight(source.Weight),
                ParseCost(source.Cost),
                ItemRarity.Common, // Standard armor is common
                category,
                source.BaseAc,
                source.PlusDexMod,
                source.PlusMax ?? dexCap,
                source.StrengthRequirement ?? 0,
                source.StealthDisadvantage
            );
            return armor;
        }

        public static IItem MapMagicItem(Open5eMagicItem source)
        {
            ArgumentNullException.ThrowIfNull(source);

            // Need a Generic Magic Item implementation or map to Weapon/Armor if applicable?
            // Since Open5e separates "magicitems" from "weapons", a "Weapon +1" in magicitems is complex.
            // For now, map as Generic Item with Rarity.
            
            var item = new StandardItem(
                Guid.NewGuid(),
                source.Name,
                source.Desc,
                0, // Weight often missing in magicitems endpoint or inside Desc
                0, // Value variable
                ParseRarity(source.Rarity),
                ParseType(source.Type)
            );
            return item;
        }

        private static double ParseWeight(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            // "2 lb."
            var parts = input.Split(' ');
            if (double.TryParse(parts[0], out double val)) return val;
            return 0;
        }

        private static int ParseCost(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            // "10 gp"
            // Convert to cp? Or keep as gold value? Interface says `int Value`. Let's assume Gold for now.
            var parts = input.Split(' ');
            if (int.TryParse(parts[0], out int val))
            {
                if (input.Contains("sp", StringComparison.OrdinalIgnoreCase)) return (int)(val * 0.1);
                if (input.Contains("cp", StringComparison.OrdinalIgnoreCase)) return (int)(val * 0.01);
                if (input.Contains("pp", StringComparison.OrdinalIgnoreCase)) return (int)(val * 10);
                return val; // gp
            }
            return 0;
        }

        private static DamageType ParseDamageType(string input)
        {
             if (Enum.TryParse<DamageType>(input, true, out var result)) return result;
             return DamageType.Bludgeoning; // Default
        }

        private static System.Collections.Generic.List<WeaponProperty> ParseWeaponProperties(System.Collections.Generic.IList<string>? props)
        {
            var result = new List<WeaponProperty>();
            if (props == null) return result;

            foreach (var p in props)
            {
                // "light", "finesse", "thrown (range 20/60)"
                var clean = p.Split(' ')[0]; // Take first word
                if (Enum.TryParse<WeaponProperty>(clean, true, out var prop))
                {
                    result.Add(prop);
                }
            }
            return result;
        }

        private static ArmorCategory ParseArmorCategory(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return ArmorCategory.Light; // Default?
            if (input.Contains("Medium", StringComparison.OrdinalIgnoreCase)) return ArmorCategory.Medium;
            if (input.Contains("Heavy", StringComparison.OrdinalIgnoreCase)) return ArmorCategory.Heavy;
            if (input.Contains("Shield", StringComparison.OrdinalIgnoreCase)) return ArmorCategory.Shield;
            return ArmorCategory.Light;
        }

        private static ItemRarity ParseRarity(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return ItemRarity.Unknown;
            if (Enum.TryParse<ItemRarity>(input.Replace(" ", "", StringComparison.Ordinal), true, out var result)) return result;
            return ItemRarity.Unknown;
        }

        private static ItemType ParseType(string input)
        {
             if (string.IsNullOrWhiteSpace(input)) return ItemType.Other;
             if (input.Contains("Weapon", StringComparison.OrdinalIgnoreCase)) return ItemType.Weapon;
             if (input.Contains("Armor", StringComparison.OrdinalIgnoreCase)) return ItemType.Armor;
             if (input.Contains("Potion", StringComparison.OrdinalIgnoreCase)) return ItemType.Potion;
             if (input.Contains("Ring", StringComparison.OrdinalIgnoreCase)) return ItemType.Ring;
             if (input.Contains("Scroll", StringComparison.OrdinalIgnoreCase)) return ItemType.Scroll;
             if (input.Contains("Wondrous", StringComparison.OrdinalIgnoreCase)) return ItemType.WondrousItem;
             return ItemType.Other;
        }
    }
}
