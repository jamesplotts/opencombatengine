using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            var properties = ParseWeaponProperties(source.Properties);
            var weapon = new StandardWeapon(
                Guid.NewGuid(),
                source.Name,
                $"Category: {source.Category}",
                ParseWeight(source.Weight),
                ParseCost(source.Cost),
                ItemRarity.Common, // Standard weapons are common
                source.DamageDice,
                ParseDamageType(source.DamageType),
                properties,
                ParseWeaponRange(source.Properties, properties)
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

            // The Open5e magicitems endpoint only gives us name/slug/desc/type/rarity/
            // requires_attunement - no structured weight, cost, charges, recharge, or bonus
            // data (unlike the 5e.tools-style JsonMagicItemImporter DTO). We build a real
            // IMagicItem (rather than degrading to a plain StandardItem) so attunement works,
            // and best-effort extract charges/recharge from the free-text description, since
            // SRD item descriptions consistently phrase those in a small number of ways.
            bool requiresAttunement = source.RequiresAttunement.Contains("attunement", StringComparison.OrdinalIgnoreCase);
            var (maxCharges, rechargeFrequency, rechargeFormula) = ParseChargesAndRecharge(source.Desc);

            string rechargeRate = maxCharges > 0 && rechargeFormula.Length > 0
                ? $"{rechargeFormula} {DescribeFrequency(rechargeFrequency)}".Trim()
                : string.Empty;

            return new MagicItem(
                source.Name,
                source.Desc,
                weight: 0, // Not provided by the Open5e magicitems endpoint
                value: 0,  // Not provided by the Open5e magicitems endpoint
                ParseType(source.Type),
                requiresAttunement,
                maxCharges: maxCharges,
                rechargeRate: rechargeRate,
                rechargeFrequency: rechargeFrequency,
                rechargeFormula: rechargeFormula,
                rarity: ParseRarity(source.Rarity)
            );
        }

        private static readonly Regex ChargesRegex = new(
            @"\b(?:has|with)\s+(\d+)\s+charges\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RechargeFormulaRegex = new(
            @"regains?\s+(\d+d\d+(?:\s*[+-]\s*\d+)?)\s+(?:of\s+its\s+)?(?:expended\s+)?charges",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static (int MaxCharges, RechargeFrequency Frequency, string Formula) ParseChargesAndRecharge(string desc)
        {
            if (string.IsNullOrWhiteSpace(desc)) return (0, RechargeFrequency.Unspecified, string.Empty);

            var chargesMatch = ChargesRegex.Match(desc);
            int maxCharges = chargesMatch.Success && int.TryParse(chargesMatch.Groups[1].Value, out int parsed) ? parsed : 0;
            if (maxCharges == 0) return (0, RechargeFrequency.Unspecified, string.Empty);

            var frequency = RechargeFrequency.Unspecified;
            if (desc.Contains("dawn", StringComparison.OrdinalIgnoreCase)) frequency = RechargeFrequency.Dawn;
            else if (desc.Contains("dusk", StringComparison.OrdinalIgnoreCase)) frequency = RechargeFrequency.Dusk;
            else if (desc.Contains("midnight", StringComparison.OrdinalIgnoreCase)) frequency = RechargeFrequency.Midnight;
            else if (desc.Contains("short rest", StringComparison.OrdinalIgnoreCase)) frequency = RechargeFrequency.ShortRest;
            else if (desc.Contains("long rest", StringComparison.OrdinalIgnoreCase)) frequency = RechargeFrequency.LongRest;

            var formulaMatch = RechargeFormulaRegex.Match(desc);
            string formula = formulaMatch.Success ? formulaMatch.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal) : string.Empty;

            return (maxCharges, frequency, formula);
        }

        private static string DescribeFrequency(RechargeFrequency frequency) => frequency switch
        {
            RechargeFrequency.Dawn => "at dawn",
            RechargeFrequency.Dusk => "at dusk",
            RechargeFrequency.Midnight => "at midnight",
            RechargeFrequency.ShortRest => "per short rest",
            RechargeFrequency.LongRest => "per long rest",
            _ => string.Empty
        };

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
            // "10 gp" — IItem.Value is denominated in copper pieces (the
            // finest SRD unit), so every real price survives exactly. A
            // gold-denominated int previously truncated anything under 1 gp
            // to zero via integer truncation (e.g. "1 cp" -> 0).
            var parts = input.Split(' ');
            if (int.TryParse(parts[0], out int val))
            {
                if (input.Contains("sp", StringComparison.OrdinalIgnoreCase)) return val * 10;
                if (input.Contains("cp", StringComparison.OrdinalIgnoreCase)) return val;
                if (input.Contains("pp", StringComparison.OrdinalIgnoreCase)) return val * 1000;
                return val * 100; // gp
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

        // "thrown (range 20/60)", "ammunition (range 80/320)" — Open5e embeds a
        // weapon's normal/long range as the first/second numbers in a
        // parenthetical suffix on the Thrown/Ammunition property string
        // itself. ParseWeaponProperties above already strips this suffix
        // (it only keeps the leading word to match a WeaponProperty enum
        // name), silently discarding the range — this recovers it instead
        // of leaving IWeapon.Range at a guessed default whenever real data
        // is actually present, the same "parse it for real" principle
        // Open5eSpellTextParser already applies to spell damage/saves. Only
        // the first ("normal") range number is used; the "long" range
        // (disadvantage-beyond-normal) is not modeled.
        private static readonly Regex WeaponRangeRegex = new(
            @"\(range\s+(\d+)(?:\s*/\s*\d+)?\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static int ParseWeaponRange(System.Collections.Generic.IList<string>? rawProps, System.Collections.Generic.List<WeaponProperty> parsedProps)
        {
            if (rawProps != null)
            {
                foreach (var p in rawProps)
                {
                    var match = WeaponRangeRegex.Match(p);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedRange))
                    {
                        return parsedRange;
                    }
                }
            }

            // No parseable "(range X/Y)" text — fall back to a real SRD
            // default rather than a hardcoded per-weapon table: 10 feet for
            // a Reach weapon, 5 feet otherwise.
            return parsedProps.Contains(WeaponProperty.Reach) ? 10 : 5;
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
